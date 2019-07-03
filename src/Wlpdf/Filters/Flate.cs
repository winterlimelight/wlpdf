using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Wlpdf.Types;

namespace Wlpdf.Filters
{
    public class Flate : IFilter
    {
        public const string Name = "/FlateDecode";

        private int _predictor = 1; // defaults to 1
        private int _columns = 0;

        public Flate(PdfDictionary decodeParams)
        {
            if (decodeParams != null)
            {
                if (decodeParams.ContainsKey("/Predictor"))
                    _predictor = (decodeParams["/Predictor"] as PdfSimple<int>).Value;
                if (decodeParams.ContainsKey("/Columns"))
                    _columns = (decodeParams["/Columns"] as PdfSimple<int>).Value;
            }
        }

        public byte [] Decode(byte[] compressedBytes)
        {
            byte[] decompressedBytes;
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                // Using SharpZipLib due to mismatch between RFC1950 and 1951 - see:
                // https://stackoverflow.com/questions/18450297/is-it-possible-to-use-the-net-deflatestream-for-pdf-creation
                // and https://benfoster.io/blog/zlib-compression-net-core
                using (var compressedStream = new MemoryStream(compressedBytes))
                using (var decompressionStream = new InflaterInputStream(compressedStream))
                    decompressionStream.CopyTo(decompressedStream);

                decompressedBytes = decompressedStream.ToArray();
            }

            if (_predictor > 1 && _columns > 0)
                return RemoveDifference(decompressedBytes);
            return decompressedBytes;
        }

        public byte[] Encode(byte [] bytes)
        {
            var deflater = new Deflater(Deflater.BEST_COMPRESSION, false); // false will include header (RFC1950)

            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (var decompressedStream = new MemoryStream(bytes))
                using (var compressionStream = new DeflaterOutputStream(compressedStream, deflater))
                    decompressedStream.CopyTo(compressionStream);

                return compressedStream.ToArray();
            }
        }

        private byte[] RemoveDifference(byte[] decompressed)
        {
            // 7.4.4.4 A Predictor value >= 10 shall indicate that a PNG predictor is in use;
            if (_predictor < 10)
                throw new Exception("Predictor is not presently supported");

            int bytesPerRow = _columns + 1;
            int rowCount = decompressed.Length / bytesPerRow;
            byte[] bytes = new byte[_columns * rowCount];

            for (int r = 0; r < rowCount; r++)
            {
                int offset = bytesPerRow * r;
                // 7.4.4.4 The specific predictor function used shall be explicitly encoded in the incoming data.
                switch (decompressed[offset])
                {
                    case 2: // PNG Up: Up(x) + Prior(x)
                        for (int c = 0; c < _columns; c++)
                        {
                            byte src = decompressed[bytesPerRow * r + 1 + c];
                            if (r == 0)
                                bytes[_columns * r + c] = src;
                            else
                                bytes[_columns * r + c] = (byte)((src + bytes[_columns * (r - 1) + c]) % 256);
                        }
                        break;

                }
                //if (i == 0 || (_columns > 0 && i % _columns == 0))
                //    continue; // leave alone the first byte in a 'column'

                //bytes[i] = (byte)(bytes[i - 1] + bytes[i] % 256);
            }
            

            // TODO for PNG filters: https://tools.ietf.org/html/rfc2083#section-6
            return bytes;
        }
    }
}

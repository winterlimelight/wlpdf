using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class XObject : IPdfTypedObject, IPdfStream
    {
        private PdfStream _stream;

        public XObject()
        {
            _stream = new PdfStream(new PdfDictionary());
            _stream.Dict["/Type"] = new PdfName() { Name = TypeName };
        }

        public void SetImage(Image img)
        {
            _stream.Dict["/Subtype"] = new PdfName() { Name = "/Image" };
            _stream.Dict["/Width"] = new PdfNumeric(img.Width);
            _stream.Dict["/Height"] = new PdfNumeric(img.Height);
            if (img.PixelFormat == PixelFormat.Format32bppArgb || img.PixelFormat == PixelFormat.Format32bppRgb)
            {
                _stream.Dict["/ColorSpace"] = new PdfName() { Name = "/DeviceRGB" };
                _stream.Dict["/BitsPerComponent"] = new PdfNumeric(8);

                var streamBytes = new List<byte>();
                var smaskBytes = new List<byte>();
                Bitmap bmp = new Bitmap(img);
                for (int r = 0; r < bmp.Height; r++)
                    for (int c = 0; c < bmp.Width; c++)
                    {
                        Color color = bmp.GetPixel(c, r);                       
                        streamBytes.Add(color.R);
                        streamBytes.Add(color.G);
                        streamBytes.Add(color.B);

                        smaskBytes.Add(color.A);
                    }
                _stream.SetStream(streamBytes.ToArray());

                // TODO /Filter [ /RunLengthDecode ] - need to add encoding support.

                if (smaskBytes.Any(b => b != 255))
                {
                    // create alpha-mask
                    var smaskStream = new PdfStream(new PdfDictionary());
                    var smask = new XObject(smaskStream);
                    smaskStream.SetStream(smaskBytes.ToArray());

                    smask.Dict["/Subtype"] = new PdfName() { Name = "/Image" };
                    smask.Dict["/Width"] = new PdfNumeric(img.Width);
                    smask.Dict["/Height"] = new PdfNumeric(img.Height);
                    smask.Dict["/ColorSpace"] = new PdfName() { Name = "/DeviceGray" };
                    smask.Dict["/BitsPerComponent"] = new PdfNumeric(8);

                    _stream.Dict["/SMask"] = smask;
                }
            }
            else
                throw new Exception("Image format not supported");
        }

        public XObject(PdfStream stream)
        {
            _stream = stream;
        }

        public string TypeName { get { return "/XObject"; } }
        public PdfDictionary Dict { get { return _stream.Dict;  } }

        public byte[] GetEncoded()
        {
            return _stream.GetEncoded();
        }
    }
}

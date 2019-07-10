using System;
using System.Linq;
using Wlpdf.Filters;

namespace Wlpdf.Types.Basic
{
    public class PdfStream : IPdfStream
    {
        public byte[] Stream { get; private set; }
        public PdfDictionary Dict { get; private set; }

        public PdfStream(PdfDictionary dict)
        {
            Dict = dict;
        }

        public byte[] GetEncoded()
        {
            byte[] data = Stream;

            var filters = GetFilters();
            for (int i = filters.Length - 1; i >= 0; i--)
                data = filters[i].Encode(data);
            return data;
        }

        public void UpdateStream(byte[] data)
        {
            Stream = data;
        }

        public void SetStream(byte[] data)
        {
            Dict.Remove(new PdfName() { Name = "/Length" });

            foreach (var filter in GetFilters())
                data = filter.Decode(data);
            Stream = data;

            // TEMP - we haven't built encoding handling for difference filtering
            Dict.Remove(new PdfName() { Name = "/DecodeParms" });
        }

        private IFilter GetFilter(PdfName pdfName)
        {
            PdfDictionary decodeParams = null;
            if (Dict.ContainsKey("/DecodeParms"))
                decodeParams = Dict["/DecodeParms"] as PdfDictionary;

            switch (pdfName.Name)
            {
                case Flate.Name: return new Flate(decodeParams);
                case RunLength.Name: return new RunLength();
                default: throw new Exception($"Filter {pdfName.Name} unknown");
            }
        }

        private IFilter[] GetFilters()
        {
            if (!Dict.ContainsKey("/Filter"))
                return Array.Empty<IFilter>();

            var filterObj = Dict.Get<IPdfObject>("/Filter");
            if (filterObj is PdfArray)
                return (filterObj as PdfArray).Cast<PdfName>().Select(n => GetFilter(n)).ToArray();
            else if (filterObj is PdfName)
                return new IFilter[] { GetFilter(filterObj as PdfName) };

            return Array.Empty<IFilter>();
        }
    }
}

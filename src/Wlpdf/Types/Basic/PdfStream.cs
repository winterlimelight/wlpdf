using System;
using System.Linq;
using Wlpdf.Filters;

namespace Wlpdf.Types.Basic
{
    public class PdfStream : PdfDictionary, IPdfStream
    {
        public byte[] Stream { get; private set; }

        internal IFilter[] Filters { get; private set; }

        public virtual void Copy(PdfStream stream)
        {
            Stream = stream.Stream;
            Filters = stream.Filters;
            base.Copy(stream);
        }

        public PdfDictionary Dict { get => this; }

        public byte[] GetEncoded()
        {
            byte[] data = Stream;
            if (Filters != null)
                for (int i = Filters.Length - 1; i >= 0; i--)
                    data = Filters[i].Encode(data);
            return data;
        }

        public void UpdateStream(byte[] data)
        {
            Stream = data;
        }

        public void SetStream(byte[] data)
        {
            Remove(new PdfName() { Name = "/Length" });
            Stream = data;

            if (!ContainsKey("/Filter"))
                return;

            var filterObj = Get<IPdfObject>("/Filter");
            if (filterObj is PdfArray)
                Filters = (filterObj as PdfArray).Cast<PdfName>().Select(n => GetFilter(n)).ToArray();
            else if (filterObj is PdfName)
                Filters = new IFilter[] { GetFilter(filterObj as PdfName) };

            foreach (var filter in Filters)
                data = filter.Decode(data);
            Stream = data;

            // TEMP - we haven't built encoding handling for difference filtering
            Remove(new PdfName() { Name = "/DecodeParms" });
        }

        private IFilter GetFilter(PdfName pdfName)
        {
            PdfDictionary decodeParams = null;
            if (ContainsKey("/DecodeParms"))
                decodeParams = this["/DecodeParms"] as PdfDictionary;

            switch (pdfName.Name)
            {
                case Flate.Name: return new Flate(decodeParams);
                case RunLength.Name: return new RunLength();
                default: throw new Exception($"Filter {pdfName.Name} unknown");
            }
        }
    }
}

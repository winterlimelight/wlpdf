using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class Font : PdfDictionary, IPdfObject
    {
        private UnicodeCMap _toUnicode;

        public Font(PdfName subtype, PdfName baseFont)
        {
            this["/Type"] = new PdfName() { Name = "/Font" };
            this["/Subtype"] = subtype;
            this["/BaseFont"] = baseFont;
        }

        public Font(PdfDictionary dict) : base(dict) { }

        private UnicodeCMap ToUnicode
        {
            get
            {
                if (!ContainsKey("/ToUnicode"))
                    return null;
                if (_toUnicode == null)
                    _toUnicode = new UnicodeCMap(GetReferencedObject<PdfStream>("/ToUnicode"));
                return _toUnicode;
            }
        }

        public string BaseFont { get { return Get<PdfName>("/BaseFont")?.Name; } }
        public string SubType { get { return Get<PdfName>("/Subtype")?.Name; } }

        public string GetHexCharCode(int glyph)
        {
            if(ToUnicode != null)
            {
                if (!ToUnicode.ContainsGlyph(glyph))
                    throw new InvalidOperationException("Glyphs cannot be added to an existing CMap");
                return (new PdfHexString(ToUnicode.FromGlyph(glyph))).Text;
            }
            return null;
        }
    }
}

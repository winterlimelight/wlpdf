using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;

namespace Wlpdf.Types
{
    public class Font : IPdfTypedObject
    {
        private UnicodeCMap _toUnicode;

        public Font(PdfName subtype, PdfName baseFont)
        {
            Dict = new PdfDictionary
            {
                ["/Type"] = new PdfName() { Name = "/Font" },
                ["/Subtype"] = subtype,
                ["/BaseFont"] = baseFont
            };
        }

        public Font(PdfDictionary dict)
        {
            Dict = dict;
        }

        public string TypeName { get { return "/Font"; } }
        public PdfDictionary Dict { get; private set; }


        private UnicodeCMap ToUnicode
        {
            get
            {
                if (!Dict.ContainsKey("/ToUnicode"))
                    return null;
                if (_toUnicode == null)
                    _toUnicode = new UnicodeCMap(Dict.GetReferencedObject<PdfStream>("/ToUnicode"));
                return _toUnicode;
            }
        }

        public string BaseFont { get { return Dict.Get<PdfName>("/BaseFont")?.Name; } }
        public string SubType { get { return Dict.Get<PdfName>("/Subtype")?.Name; } }

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

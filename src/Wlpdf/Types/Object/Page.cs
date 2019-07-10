using System;
using System.Collections.Generic;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;

namespace Wlpdf.Types
{
    public class Page : IPdfTypedObject
    {
        private ContentStream _contents;

        public ContentStream Contents
        {
            get
            {
                if (_contents == null)
                    ParseContents();
                return _contents;
            }
        }

        public Page(PdfDictionary dict)
        {
            Dict = dict;
        }

        public string TypeName { get { return "/Page"; } }
        public PdfDictionary Dict { get; private set; }

        private void ParseContents()
        {
            _contents = new ContentStream(Dict.GetReferencedObject<PdfStream>("/Contents"));
        }

        public PdfRectangle MediaBox
        {
            get { return new PdfRectangle(Dict.Get<PdfArray>("/MediaBox")); }
        }

        public string AddXObject(PdfReference xref)
        {
            var resources = Dict.Get<PdfDictionary>("/Resources");
            var xobjects = resources.Get<PdfDictionary>("/XObject");

            string key = "/Xo" + xref.ObjectNumber;
            xobjects[key] = xref;
            return key;
        }
    }
}

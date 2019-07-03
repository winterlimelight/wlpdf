using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class Page : PdfDictionary, IPdfObject
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

        public Page(PdfDictionary dict) : base(dict) { }

        private void ParseContents()
        {
            _contents = new ContentStream(GetReferencedObject<PdfStream>("/Contents"));
        }
    }
}

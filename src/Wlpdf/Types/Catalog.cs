using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class Catalog : PdfDictionary, IPdfObject
    {
        public Catalog(PdfDictionary dict) : base(dict) { }
    }
}

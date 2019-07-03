using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class Pages : PdfDictionary, IPdfObject
    {
        public Pages(PdfDictionary dict) : base(dict) { }
    }
}

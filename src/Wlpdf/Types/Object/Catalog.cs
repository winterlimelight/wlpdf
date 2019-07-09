using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class Catalog : PdfDictionary, IPdfObject
    {
        public Catalog(PdfDictionary dict) : base(dict) { }
    }
}

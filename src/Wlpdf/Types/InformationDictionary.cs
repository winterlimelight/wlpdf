using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class InformationDictionary : PdfDictionary, IPdfObject
    {
        public InformationDictionary(PdfDictionary dict) : base(dict) { }
    }
}

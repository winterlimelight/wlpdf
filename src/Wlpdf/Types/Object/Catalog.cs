using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class Catalog : IPdfTypedObject
    {
        public Catalog(PdfDictionary dict)
        {
            Dict = dict;
        }

        public string TypeName { get { return "/Catalog"; } }
        public PdfDictionary Dict { get; private set; }
    }
}

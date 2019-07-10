using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class InformationDictionary : IPdfTypedObject
    {
        public InformationDictionary(PdfDictionary dict)
        {
            Dict = dict;
        }

        public string TypeName { get { return "/Catalog"; } }
        public PdfDictionary Dict { get; private set; }

    }
}

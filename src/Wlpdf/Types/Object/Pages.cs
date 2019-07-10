using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class Pages : IPdfTypedObject
    {
        public Pages(PdfDictionary dict)
        {
            Dict = dict;
        }

        public string TypeName { get { return "/Pages"; } }
        public PdfDictionary Dict { get; private set; }
    }
}

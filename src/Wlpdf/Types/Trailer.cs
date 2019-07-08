using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class Trailer : PdfDictionary
    {
        public PdfReference Info { get; }
        public PdfReference Root { get; }

        public Trailer(PdfDictionary dict)
        {
            foreach (var kvp in dict)
                this[kvp.Key] = kvp.Value;

            if (dict.ContainsKey("/Info"))
                Info = dict["/Info"] as PdfReference;
            Root = dict["/Root"] as PdfReference;
        }
    }
}

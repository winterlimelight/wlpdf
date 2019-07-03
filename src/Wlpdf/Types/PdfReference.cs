using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class PdfReference : IPdfObject
    {
        private PdfDocument _doc;

        public int ObjectNumber { get; set; }
        public int Generation { get; set; }

        internal PdfReference(PdfDocument doc)
        {
            _doc = doc;
        }

        public IPdfObject GetTarget()
        {
            return _doc.ResolveReference(this);
        }

        public override string ToString()
        {
            return $"{ObjectNumber} {Generation} R";
        }
    }
}

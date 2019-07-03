using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class PdfSimple<T> : IPdfObject
    {
        public T Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

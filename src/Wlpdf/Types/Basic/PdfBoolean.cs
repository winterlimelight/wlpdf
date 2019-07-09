using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types.Basic
{
    public class PdfBoolean : IPdfObject
    {
        private bool _value;

        public PdfBoolean(bool val)
        {
            _value = val;
        }

        public static implicit operator bool(PdfBoolean v) { return v._value; }
    }
}

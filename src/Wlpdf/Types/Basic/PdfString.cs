using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types.Basic
{
    public class PdfString : IPdfObject
    {
        private string _value;

        public PdfString(string val)
        {
            _value = val;
        }

        public static implicit operator string(PdfString v) { return v._value; }

        public override string ToString()
        {
            return _value;
        }
    }
}

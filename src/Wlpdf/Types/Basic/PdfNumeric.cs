using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types.Basic
{
    public class PdfNumeric : IPdfObject
    {
        private double _value;

        public PdfNumeric(int val)
        {
            _value = val;
            IsInteger = true;
        }

        public PdfNumeric(double val)
        {
            _value = val;
            IsInteger = false;
        }

        public bool IsInteger { get; private set; }

        public static implicit operator int(PdfNumeric v) { return (int)v._value; }

        public static implicit operator double(PdfNumeric v) { return v._value; }

        public override string ToString()
        {
            return IsInteger ? ((int)_value).ToString() : _value.ToString();
        }
    }
}

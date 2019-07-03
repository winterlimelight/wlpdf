using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    // TODO feels backwards storing as the string and converting to/from number given string representation is only used at load/save
    public class PdfHexString : IPdfObject
    {
        public PdfHexString() { }

        public PdfHexString(int value)
        {
            Text = value.ToString("X4");
        }

        public string Text { get; set; }

        public int ToInt
        {
            get
            {
                return int.Parse(Text, System.Globalization.NumberStyles.HexNumber);
            }
        }
    }
}

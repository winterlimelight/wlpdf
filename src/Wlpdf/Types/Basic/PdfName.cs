using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types.Basic
{
    public class PdfName : IPdfObject
    {
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is PdfName))
                return false;
            return Name.Equals((obj as PdfName).Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        // Automatic conversion from string to PdfName
        public static implicit operator PdfName(string s)
        {
            return new PdfName() { Name = s };
        }
    }
}

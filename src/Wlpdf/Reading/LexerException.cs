using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Reading
{
    public class LexerException : Exception
    {
        public LexerException(string message) : base(message) { }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Reading
{
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message) { }
    }
}

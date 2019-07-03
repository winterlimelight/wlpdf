using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class InvalidStructureException : Exception
    {
        public string FieldName { get; set; }
        public InvalidStructureException(string message) : base(message) { }
    }
}

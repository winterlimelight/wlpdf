using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public interface IPdfTypedObject : IPdfObject
    {
        string TypeName { get; }
        PdfDictionary Dict { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public interface IPdfStream : IPdfObject
    {
        byte[] GetEncoded();
        PdfDictionary Dict { get; } // TODO IPdfDictionary or ITypedObject interface?
    }
}

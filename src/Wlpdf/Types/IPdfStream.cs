using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public interface IPdfStream : IPdfObject
    {
        void SetFromStream(PdfStream stream);
    }
}

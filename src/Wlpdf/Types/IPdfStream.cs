using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public interface IPdfStream : IPdfObject
    {
        void SetFromStream(PdfStream stream);
    }
}

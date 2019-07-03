using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public class XObject : PdfStream, IPdfStream
    {
        public void SetFromStream(PdfStream stream)
        {
            Copy(stream);
        }
    }
}

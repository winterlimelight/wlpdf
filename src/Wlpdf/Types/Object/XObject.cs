using System;
using System.Collections.Generic;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class XObject : IPdfTypedObject, IPdfStream
    {
        private PdfStream _stream;

        public XObject(PdfStream stream)
        {
            _stream = stream;
        }

        public string TypeName { get { return "/XObject"; } }
        public PdfDictionary Dict { get { return _stream as PdfDictionary;  } }

        public byte[] GetEncoded()
        {
            return _stream.GetEncoded();
        }
    }
}

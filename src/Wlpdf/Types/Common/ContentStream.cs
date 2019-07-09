using System;
using System.Linq;
using System.Text;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types.Common
{
    public class ContentStream
    {
        private PdfStream _stream;

        public ContentStream(PdfStream stream)
        {
            _stream = stream;
        }

        public override string ToString()
        {
            return new string(Encoding.ASCII.GetChars(_stream.Stream));
        }

        // in future we'd expect to have a list of objects within the stream. For now, just allow string set
        public void SetContent(string content)
        {
            _stream.UpdateStream(Encoding.ASCII.GetBytes(content));
        }

        // PDF1.7 sect 8.2:
        // Data in a content stream is a sequence of operands and operations in postfix notation (i.e. [operand]* operator).
        // Operands are expressed as basic data objects according to PDF syntax.
        // Five types: path object, text object, external (XObject) object, inline image, shading object

        // So I'm thinking we can use the existing lexer, storing operands until we hit a non-object-string which we treat as an operator
        // TBH, we could start by just trying to manually manipulate the string and see what happens... i.e. can we add transparent and/or opaque text?

    }
}

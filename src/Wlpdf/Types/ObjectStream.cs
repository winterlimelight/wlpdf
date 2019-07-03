using System;
using System.Collections.Generic;
using System.Linq;

namespace Wlpdf.Types
{
    public class ObjectStream : PdfStream, IPdfStream
    {
        private readonly PdfDocument _doc;
        private IPdfObject[] _objects;

        public ObjectStream(PdfDocument doc)
        {
            _doc = doc;
        }

        public void SetFromStream(PdfStream stream)
        {
            Copy(stream);
            Stream = stream.Stream;
            // TODO write .Stream - if we added to the ObjectStream we'd need to update Stream

            var parser = new Reading.Parser(new Reading.Lexer().SetSource(stream.Stream), _doc);
            _objects = parser.ParseObjectStream().ToArray();
        }

        public IPdfObject this[int index]
        {
            get
            {
                return _objects[index];
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Wlpdf.Types.Basic;

namespace Wlpdf.Types
{
    public class ObjectStream : IPdfTypedObject, IPdfStream
    {
        private readonly PdfDocument _doc;
        private PdfStream _stream;
        private IPdfObject[] _objects;

        public ObjectStream(PdfDocument doc, PdfStream stream)
        {
            _doc = doc;
            _stream = stream;

            var parser = new Reading.Parser(new Reading.Lexer().SetSource(_stream.Stream), _doc);
            _objects = parser.ParseObjectStream().ToArray();
        }

        public string TypeName { get { return "/ObjStm"; } }
        public PdfDictionary Dict { get { return _stream.Dict; } }

        // TODO write _stream - if we added to the ObjectStream we'd need to update Stream
        public byte[] GetEncoded()
        {
            return _stream.GetEncoded();
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

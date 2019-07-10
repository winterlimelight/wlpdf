using System;
using System.Collections.Generic;
using Wlpdf.Types;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;
using Wlpdf.Types.Object;

namespace Wlpdf.Reading
{
    class Parser
    {
        private readonly Lexer _lexer;
        private readonly PdfDocument _doc;

        internal Parser(Lexer lexer, PdfDocument doc)
        {
            _lexer = lexer;
            _doc = doc;
        }

        internal void ParseDocument()
        {
            _doc.Version = ParseHeader();

            _lexer.SeekToXref();

            _lexer.Next();

            if (Accept(TokenType.Xref))
            {
                _doc.Xref = new XrefObject(ParseXref());

                // ParseXref overreads, so no need to _lexer.Next() here
                Expect(TokenType.Trailer);

                _lexer.Next();
                Expect(TokenType.Dict);
                _doc.Trailer = new Trailer(ParseDictionary());
            }
            else if (IsIndirectObjectDefinition())
            {
                _doc.Xref = ParseIndirectObjectDefinition(null).Object as XrefObject;
                _doc.Trailer = new Trailer((_doc.Xref as IPdfTypedObject).Dict);
            }
            else
                throw new ParserException("No cross-reference table found");

            // Load all the indirect objects. If they are out of order then PdfDocument reference handling will lazy load dependents
            foreach (PdfCrossReference obj in _doc.GetObjects())
                ParseIndirectObjectDefinition(obj);
        }

        internal IEnumerable<IPdfObject> ParseObjectStream()
        {
            var objects = new List<IPdfObject>();
            IPdfObject obj;
            while ((obj = ParseObject()) != null)
                objects.Add(obj);
            return objects;
        }

        private string ParseHeader()
        {
            _lexer.Next();
            Expect(TokenType.MagicNumber);
            return (string)_lexer.Current.Value;
        }

        private IEnumerable<PdfCrossReference> ParseXref()
        {
            // If an xref entry is in use (rather than a stream):
            // The table shall contain a one line entry for each indirect object, specifying the byte offset within the body of the file
            // Sets of entries are preceded by subsection line with format: x y where x is obj id of first entry and y is number of entries. Object Ids are sequential.
            // Each entry is twenty bytes long including EOL with following format:
            // nnnnnnnnnn ggggg n eol where n is 10-digit offset into decoded file-stream, g - generation number, n - n for in-use f for free
            var entries = new List<PdfCrossReference>();

            _lexer.Next();
            while (Accept(TokenType.Integer))
            {
                int objectId = (int)_lexer.Current.Value;

                _lexer.Next();
                Expect(TokenType.Integer);
                int count = (int)_lexer.Current.Value;

                for (int i = 0; i < count; i++)
                {
                    _lexer.Next();
                    Expect(TokenType.Integer);
                    int offset = (int)_lexer.Current.Value;

                    _lexer.Next();
                    Expect(TokenType.Integer);
                    int generation = (int)_lexer.Current.Value;

                    _lexer.Next();
                    Expect(TokenType.NonObjectString);
                    string inUse = (string)_lexer.Current.Value;

                    entries.Add(new PdfCrossReference
                    {
                        EntryType = inUse == "f" ? XrefEntryType.Free : XrefEntryType.Used,
                        ObjectNumber = objectId + i,
                        Generation = generation,
                        Offset = offset
                    });
                }
                _lexer.Next();
            }
            return entries;
        }

        internal PdfCrossReference ParseIndirectObjectDefinition(PdfCrossReference indirectObj)
        {
            int offset = -1;
            if (indirectObj == null)
                offset = _lexer.Current.Offset;
            else
            {
                if (indirectObj.Loaded)
                    return indirectObj;

                // Entry points to an indirect object stream - resolve that.
                if (indirectObj != null && indirectObj.EntryType == XrefEntryType.InStream)
                    return ParseFromStream(indirectObj);

                _lexer.Seek(indirectObj.Offset);
                _lexer.Next();
                Expect(TokenType.Integer);
            }

            int objectNumber = (int)_lexer.Current.Value;

            _lexer.Next();
            Expect(TokenType.Integer);
            int generation = (int)_lexer.Current.Value;

            if (indirectObj == null)
                indirectObj = new PdfCrossReference() { EntryType = XrefEntryType.Used, ObjectNumber = objectNumber, Generation = generation, Offset = offset };
            else if (objectNumber != indirectObj.ObjectNumber || generation != indirectObj.Generation)
                throw new ParserException($"Expecting object {indirectObj.ObjectNumber} {indirectObj.Generation} at offset {indirectObj.Offset} but got {objectNumber} {generation}");

            _lexer.Next();
            Expect(TokenType.Object);

            IPdfObject obj = ParseObject();

            _lexer.Next();

            if (Accept(TokenType.Stream))
            {
                if (!(obj is PdfDictionary))
                    throw new ParserException("Stream objects must be preceded by a dictionary");
                obj = SetObjectType(indirectObj, ParseStream(obj as PdfDictionary));

                _lexer.Next();
            }
            else if (obj is PdfDictionary)
                obj = SetObjectType(indirectObj, obj as PdfDictionary);

            Expect(TokenType.EndObject);

            indirectObj.Object = obj;
            indirectObj.Loaded = true;
            return indirectObj;
        }

        private PdfCrossReference ParseFromStream(PdfCrossReference indirectObj)
        {
            PdfCrossReference streamObjRef = _doc.Xref.Get(indirectObj.StreamObjectNumber, 0);
            if (streamObjRef == null || streamObjRef.EntryType != XrefEntryType.Used)
                throw new Exception("Indirect object stream could not be found");

            ParseIndirectObjectDefinition(streamObjRef);
            var objectStream = streamObjRef.Object as ObjectStream;

            indirectObj.Object = objectStream[indirectObj.Offset];
            indirectObj.Loaded = true;
            return indirectObj;
        }

        // For some objects it is useful to have access to special properties for access or manipulation.
        // Those are selected from the /Type here
        private IPdfObject SetObjectType(PdfCrossReference xref, PdfDictionary dict)
        {
            if (dict.ContainsKey("/Type"))
            {
                string objType = dict.Get<PdfName>("/Type").Name;
                switch (objType)
                {
                    case "/Catalog": return new Catalog(dict);
                    case "/Pages": return new Pages(dict);
                    case "/Page": return new Page(dict);
                    case "/Font": return new Font(dict);
                }
            }

            if (xref.ObjectNumber == _doc.Trailer.Info.ObjectNumber && xref.Generation == _doc.Trailer.Info.Generation)
                return new InformationDictionary(dict);

            return dict;
        }

        private IPdfStream SetObjectType(PdfCrossReference xref, PdfStream stream)
        {
            if (!stream.Dict.ContainsKey("/Type"))
                return stream;

            string objType = stream.Dict.Get<PdfName>("/Type").Name;
            switch (objType)
            {
                case "/XRef": return new XrefObject(stream);
                case "/ObjStm": return new ObjectStream(_doc, stream);
                case "/XObject": return new XObject(stream);
                default: throw new ParserException($"Unknown stream type {objType}");
            }
        }


        private IPdfObject ParseObject()
        {
            if (_lexer.Next() == null)
                return null; // EOF

            if (Accept(TokenType.Dict))
            {
                return ParseDictionary();
            }
            else if (Accept(TokenType.Array))
            {
                return ParseArray();
            }
            else if (Accept(TokenType.Name))
            {
                return new PdfName() { Name = (string)_lexer.Current.Value };
            }
            else if (Accept(TokenType.String))
            {
                return new PdfString((string)_lexer.Current.Value);
            }
            else if (Accept(TokenType.HexString))
            {
                return new PdfHexString() { Text = (string)_lexer.Current.Value };
            }
            else if (Accept(TokenType.Real))
            {
                return new PdfNumeric((double)_lexer.Current.Value);
            }
            else if (Accept(TokenType.Integer))
            { 
                // if it's an integer we need to see if it is an int value or a reference
                int value1 = (int)_lexer.Current.Value;

                Token[] peek = _lexer.Peek(2);
                if (peek[0].Type != TokenType.Integer || peek[1].Type != TokenType.NonObjectString || peek[0].Value is double)
                    return new PdfNumeric(value1);

                // it's a reference - consume it
                _lexer.Next();
                Expect(TokenType.Integer);
                int value2 = (int)_lexer.Current.Value;

                _lexer.Next();
                Expect(TokenType.NonObjectString);

                if ("R" != (string)_lexer.Current.Value)
                    throw new Exception("Expecting a reference character 'R'.");

                return new PdfReference(_doc) { ObjectNumber = value1, Generation = value2 };
            }
            else if (Accept(TokenType.Boolean))
            {
                return new PdfBoolean((bool)_lexer.Current.Value);
            }
            else if (Accept(TokenType.Null))
                return new PdfNull();
            throw new ParserException($"Unexpected {_lexer.Current.Type} at {_lexer.Current.Offset}");
        }

        private PdfStream ParseStream(PdfDictionary dict)
        {
            if (!dict.ContainsKey("/Length"))
                throw new ParserException($"Stream is missing length information");

            var length = dict.Get<PdfNumeric>("/Length");
            if (length <= 0)
                throw new ParserException($"Stream is missing length information");

            byte[] data = _lexer.Take(length);

            var stream = new PdfStream(dict);
            stream.SetStream(data);

            _lexer.Next();
            Expect(TokenType.EndStream);

            return stream;
        }

        private PdfDictionary ParseDictionary()
        {
            var dict = new PdfDictionary();

            _lexer.Next();
            while (!Accept(TokenType.EndDict))
            {
                Expect(TokenType.Name);
                var key = new PdfName() { Name = (string)_lexer.Current.Value };

                IPdfObject obj = ParseObject();
                dict.Add(key, obj);

                _lexer.Next();
            }

            return dict;
        }

        private PdfArray ParseArray()
        {
            var ary = new PdfArray();

            Token peek = _lexer.Peek();
            while (peek.Type != TokenType.EndArray)
            {
                IPdfObject obj = ParseObject();
                ary.Add(obj);

                peek = _lexer.Peek();
            }

            _lexer.Next();
            Expect(TokenType.EndArray);

            return ary;
        }

        private void Expect(TokenType t)
        {
            if (_lexer.Current == null)
                throw new ParserException($"Expected {t} but got EOF");
            if (_lexer.Current.Type != t)
                throw new ParserException($"Expected {t} at {_lexer.Current.Offset} but got {_lexer.Current.Type}");
        }

        private bool Accept(TokenType t)
        {
            if (_lexer.Current == null)
                throw new ParserException($"Unexpected EOF while testing for {t}");
            return _lexer.Current.Type == t;
        }

        private bool IsIndirectObjectDefinition()
        {
            Token[] next = _lexer.Peek(2);
            return _lexer.Current.Type == TokenType.Integer && next[0].Type == TokenType.Integer && next[1].Type == TokenType.Object;
        }
    }
}

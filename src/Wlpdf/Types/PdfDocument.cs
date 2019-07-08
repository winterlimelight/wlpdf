using System;
using System.Collections.Generic;
using System.IO;
using Wlpdf.Reading;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;
using Wlpdf.Types.Object;

namespace Wlpdf.Types
{
    public class PdfDocument
    {
        public string Version { get; set; } = "1.7";
        internal XrefObject Xref { get; set; }
        public Trailer Trailer {get; set;}

        private byte[] _inputPdf;

        public static PdfDocument FromFile(string filename)
        {
            var doc = new PdfDocument
            {
                _inputPdf = File.ReadAllBytes(filename)
            };
            doc.Parser.ParseDocument();
            return doc;
        }

        public PdfDocument() { }

        private Parser Parser
        {
            get
            {
                if (_inputPdf == null || _inputPdf.Length <= 0)
                    throw new Exception("PdfDocument parser requires input to be set by calling FromFile()");
                var parser = new Parser(new Lexer().SetSource(_inputPdf), this);
                return parser;
            }
        }

        public void Save(string filename)
        {
            (new Writing.PdfDocumentWriter(this)).WriteToFile(filename);
        }

        public IEnumerable<PdfCrossReference> GetObjects()
        {
            return Xref.GetIndirectObjects();
        }

        public PdfCrossReference AddObject(IPdfObject obj)
        {
            return Xref.AddEntry(obj);
        }

        internal IPdfObject ResolveReference(PdfReference reference)
        {
            PdfCrossReference indirectObj = Xref.Get(reference.ObjectNumber, reference.Generation);
            if (indirectObj == null)
                return null;

            if (!indirectObj.Loaded)
                Parser.ParseIndirectObjectDefinition(indirectObj);

            IPdfObject obj = indirectObj.Object;
            if (obj is PdfReference)
                return ResolveReference(obj as PdfReference);

            return obj;
        }
    }
}

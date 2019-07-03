using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wlpdf.Types;

namespace Wlpdf.Writing
{
    public class PdfDocumentWriter
    {
        private PdfDocument _doc;
        private BinaryWriter _writer;

        public PdfDocumentWriter(PdfDocument doc)
        {
            _doc = doc;
        }

        public void WriteToFile(string filename)
        {
            using (var fileStream = File.Open(filename, FileMode.Create))
            using (_writer = new BinaryWriter(fileStream))
            {
                // header
                AsciiToOutput($"%PDF-{_doc.Version}\n");

                // body objects
                PdfCrossReference[] xrefs = _doc.GetObjects().ToArray();
                foreach (PdfCrossReference xref in xrefs)
                {
                    if (xref.Object is XrefObject)
                        continue;

                    _writer.Flush();

                    if(xref.EntryType == XrefEntryType.Used)
                        xref.Offset = (int)_writer.BaseStream.Position;

                    WriteIndirectObject(xref);
                }
                
                long startXref = _writer.BaseStream.Position;

                // xref
                bool isV1p5 = float.Parse(_doc.Version) >= 1.5;
                if (isV1p5) // >= 1.5 uses stream object
                {
                    _doc.Xref.UpdateStream();
                    int objNo = xrefs.Single(x => x.Object is XrefObject).ObjectNumber; 
                    AsciiToOutput($"{objNo} 0 obj\n");
                    WriteStreamObject(_doc.Xref);
                    AsciiToOutput("\nendobj\n");
                }
                else
                    WriteXrefSection(xrefs);
                

                AsciiToOutput("startxref\n");
                AsciiToOutput($"{startXref}\n");
                AsciiToOutput("%%EOF\n");
            }
        }

        private void WriteXrefSection(PdfCrossReference[] xrefs)
        {
            AsciiToOutput("xref\n");
            AsciiToOutput($"0 {xrefs.Length + 1}\n"); // +1 for the objectnumber 0 entry
            AsciiToOutput("0000000000 65535 f\n");
            foreach (PdfCrossReference xref in xrefs)
                AsciiToOutput($"{xref.Offset:D10} {0:D5} n\n");

            var info = _doc.Trailer.Info;
            var root = _doc.Trailer.Root;
            AsciiToOutput("trailer\n");
            AsciiToOutput("<<\n");
            AsciiToOutput($"/Size {xrefs.Length + 1}\n");
            AsciiToOutput($"/Info {info.ObjectNumber} {info.Generation} R\n");
            AsciiToOutput($"/Root {root.ObjectNumber} {root.Generation} R\n");
            AsciiToOutput(">>\n");
        }

        private void WriteIndirectObject(PdfCrossReference obj)
        {
            if (obj.EntryType != XrefEntryType.Used)
                return;

            AsciiToOutput($"{obj.ObjectNumber} {obj.Generation} obj\n");
            WriteObject(obj.Object);
            AsciiToOutput("\nendobj\n");
        }

        private void WriteObject(IPdfObject obj)
        {
            if (obj is PdfStream)
                WriteStreamObject(obj as PdfStream);
            else if (obj is PdfDictionary)
                WriteDictionary(obj as PdfDictionary);
            else if (obj is PdfArray)
                WriteArray(obj as PdfArray);
            else if (obj is PdfReference)
                WriteReference(obj as PdfReference);
            else if (obj is PdfName)
                AsciiToOutput($"{(obj as PdfName).Name}");
            else if (obj is PdfSimple<string>)
                WriteString(obj as PdfSimple<string>);
            else if (obj is PdfSimple<bool>)
                WriteBoolean(obj as PdfSimple<bool>);
            else if (obj.GetType().Name.StartsWith("PdfSimple"))
                AsciiToOutput(obj.ToString());
            else if (obj is PdfHexString)
                WriteHexString(obj as PdfHexString);
            else if (obj is PdfNull)
                AsciiToOutput("null");
            else
                throw new Exception("Unknown object while writing");
        }

        private void WriteStreamObject(PdfStream pdfStreamObject)
        {
            byte[] data = pdfStreamObject.Stream;
            if (pdfStreamObject.Filters != null)
                for (int i = pdfStreamObject.Filters.Length - 1; i >= 0; i--)
                    data = pdfStreamObject.Filters[i].Encode(data);

            pdfStreamObject.Add(new PdfName() { Name = "/Length" }, new PdfSimple<int>() { Value = data.Length });
            WriteDictionary(pdfStreamObject);

            AsciiToOutput("stream\n");
            _writer.Write(data);
            AsciiToOutput("\nendstream\n");
        }

        private void WriteDictionary(PdfDictionary pdfDictionary)
        {
            AsciiToOutput("<<");
            foreach (var kvp in pdfDictionary)
            {
                AsciiToOutput($"\n{kvp.Key} ");
                WriteObject(kvp.Value);
            }
            AsciiToOutput("\n>>");
        }

        private void WriteArray(PdfArray pdfArray)
        {
            AsciiToOutput("[");
            for (int i = 0; i < pdfArray.Count; i++)
            {
                if (i > 0)
                    AsciiToOutput(" ");
                WriteObject(pdfArray[i]);
            }
            AsciiToOutput("]");
        }

        private void WriteReference(PdfReference pdfReference)
        {
            AsciiToOutput($"{pdfReference.ObjectNumber} {pdfReference.Generation} R");
        }

        private void WriteString(PdfSimple<string> pdfSimple)
        {
            byte [] bytes;
            bool useUnicode = pdfSimple.Value.Any(c => c >= 128);
            if (useUnicode)
                bytes = System.Text.Encoding.BigEndianUnicode.GetBytes(pdfSimple.Value);
            else
                bytes = System.Text.Encoding.ASCII.GetBytes(pdfSimple.Value);

            var escaped = new List<byte>();
            escaped.Add((byte)'(');
            for (int i = 0; i < bytes.Length; i++)
            {
                byte ch = bytes[i];
                if (ch == '\\' || ch == '(' || ch == ')')
                    escaped.Add((byte)'\\');
                escaped.Add(ch);
            }
            escaped.Add((byte)')');
            _writer.Write(escaped.ToArray());
        }

        private void WriteHexString(PdfHexString pdfHexString)
        {
            AsciiToOutput($"<{pdfHexString.Text}>");
        }

        private void WriteBoolean(PdfSimple<bool> pdfSimple)
        {
            AsciiToOutput(pdfSimple.Value ? "true" : "false");
        }

        private void AsciiToOutput(string s)
        {
            _writer.Write(System.Text.Encoding.ASCII.GetBytes(s));
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wlpdf.Types;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;
using Wlpdf.Types.Object;

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
                    _writer.Flush();

                    if(xref.EntryType == XrefEntryType.Used)
                        xref.Offset = (int)_writer.BaseStream.Position;

                    if (xref.Object is XrefObject)
                        continue;

                    WriteIndirectObject(xref);
                }
                
                long startXref = _writer.BaseStream.Position;

                // xref
                if (_doc.Xref.IsStream)
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
            if (obj is IPdfStream)
                WriteStreamObject(obj as IPdfStream);
            else if (obj is IPdfTypedObject)
                WriteDictionary((obj as IPdfTypedObject).Dict);
            else if (obj is PdfDictionary)
                WriteDictionary(obj as PdfDictionary);
            else if (obj is PdfArray)
                WriteArray(obj as PdfArray);
            else if (obj is PdfReference)
                WriteReference(obj as PdfReference);
            else if (obj is PdfName)
                AsciiToOutput($"{(obj as PdfName).Name}");
            else if (obj is PdfString)
                WriteString(obj as PdfString);
            else if (obj is PdfBoolean)
                WriteBoolean(obj as PdfBoolean);
            else if (obj is PdfNumeric)
                AsciiToOutput(obj.ToString());
            else if (obj is PdfHexString)
                WriteHexString(obj as PdfHexString);
            else if (obj is PdfNull)
                AsciiToOutput("null");
            else
                throw new Exception("Unknown object while writing");
        }

        private void WriteStreamObject(IPdfStream pdfStreamObject)
        {
            byte[] data = pdfStreamObject.GetEncoded();

            pdfStreamObject.Dict.Add(new PdfName() { Name = "/Length" }, new PdfNumeric(data.Length));
            WriteDictionary(pdfStreamObject.Dict);

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

        private void WriteString(PdfString pdfString)
        {
            byte [] bytes;
            bool useUnicode = pdfString.ToString().Any(c => c >= 128);
            if (useUnicode)
                bytes = System.Text.Encoding.BigEndianUnicode.GetBytes(pdfString);
            else
                bytes = System.Text.Encoding.ASCII.GetBytes(pdfString);

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

        private void WriteBoolean(PdfBoolean pdfBoolean)
        {
            AsciiToOutput(pdfBoolean ? "true" : "false");
        }

        private void AsciiToOutput(string s)
        {
            _writer.Write(System.Text.Encoding.ASCII.GetBytes(s));
        }
    }
}

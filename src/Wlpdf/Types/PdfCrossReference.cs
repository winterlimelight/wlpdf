using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types
{
    public enum XrefEntryType { Free = 0, Used = 1, InStream = 2 }

    public class PdfCrossReference
    {
        public XrefEntryType EntryType { get; set; }
        public int ObjectNumber { get; set; }
        public int Generation { get; set; }
        public int StreamObjectNumber { get; set; } // only used by InStream
        public int Offset { get; set; } // Used = file offset,  InStream = the index of the object within the stream

        public IPdfObject Object { get; set; }
        public bool Loaded { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Wlpdf.Types.Basic;
using Wlpdf.Types.Common;

namespace Wlpdf.Types.Object
{
    internal class XrefObject : IPdfTypedObject, IPdfStream
    {
        private List<PdfCrossReference> _entries;
        private PdfStream _stream;

        public XrefObject(PdfStream stream)
        {
            _stream = stream;
            Decode(stream.Stream);
        }

        public string TypeName { get { return "/XRef"; } }
        public PdfDictionary Dict { get => _stream.Dict; }

        public XrefObject(IEnumerable<PdfCrossReference> entries)
        {
            _entries = new List<PdfCrossReference>(entries);
            // TODO write .Stream - not reqd atm as this method is for pre-1.5 - if we were 'upgrading' a version we'd need to set a number of flags then UpdateStream()
        }

        public byte[] GetEncoded()
        {
            return _stream.GetEncoded();
        }

        public PdfCrossReference AddEntry(IPdfObject target)
        {
            int newObjNum = _entries.Max(e => e.ObjectNumber) + 1;
            var xref = new PdfCrossReference()
            {
                EntryType = XrefEntryType.Used,
                ObjectNumber = newObjNum,
                Generation = 0,
                Object = target,
                Loaded = true
            };
            _entries.Add(xref);
            return xref;
        }

        public void UpdateStream()
        {
            int[] fieldWidths = GetFieldSizes();

            // TODO - at present we're assuming sequential object numbers starting from 0 - so /Index is left unchanged
            var data = new byte[_entries.Count * fieldWidths.Sum()];
            int inx = 0;
            foreach(PdfCrossReference xref in _entries)
            {
                int value1 = 0, value2 = xref.Generation;
                if (xref.EntryType == XrefEntryType.Free)
                    value1 = xref.ObjectNumber;
                else if (xref.EntryType == XrefEntryType.Used)
                    value1 = xref.Offset;
                else if (xref.EntryType == XrefEntryType.InStream)
                {
                    value1 = xref.StreamObjectNumber;
                    value2 = xref.Offset;
                }

                Util.ByteConversions.WriteBigEndianInt((int)xref.EntryType, data, inx, fieldWidths[0]);
                inx += fieldWidths[0];
                Util.ByteConversions.WriteBigEndianInt(value1, data, inx, fieldWidths[1]);
                inx += fieldWidths[1];
                Util.ByteConversions.WriteBigEndianInt(value2, data, inx, fieldWidths[2]);
                inx += fieldWidths[2];
            }
            _stream.UpdateStream(data);
        }

        internal IEnumerable<PdfCrossReference> GetIndirectObjects()
        {
            return _entries.Where(e => e.EntryType != XrefEntryType.Free); // free is ignored
        }

        internal PdfCrossReference Get(int objectNumber, int generation = 0)
        {
            return _entries.FirstOrDefault(e =>
                (e.EntryType == XrefEntryType.Used && e.ObjectNumber == objectNumber && e.Generation == generation) ||
                (e.EntryType == XrefEntryType.InStream && e.ObjectNumber == objectNumber && generation == 0)
            );
        }

        private void Decode(byte [] stream)
        {
            int[] fieldWidths = GetFieldSizes();
            int entryWidth = fieldWidths.Sum();
            if (stream.Length % entryWidth != 0)
                throw new ArgumentException("Xref stream is not a multiple of Xref field width totals");

            // 'Used' type get their object numbers from sequential ordering (done in sections)
            int[] sections = null;
            int sectionsInx = -1;
            int objNo = -1;
            if (Dict.ContainsKey("/Index"))
            {
                sections = (Dict["/Index"] as PdfArray).Select(o => (int)(o as PdfNumeric)).ToArray();
                if (sections.Length % 2 != 0)
                    throw new ArgumentException("/Index length must be even");
                sectionsInx = 0;
                objNo = sections[sectionsInx];
            }

            // create functions to pull values and convert to integers
            var getFieldFns = new Func<int, int?>[3];
            int offset = 0;
            for (int i = 0; i < 3; i++)
            {
                getFieldFns[i] = CreateDecodeNext(stream, fieldWidths[i], offset);
                offset += fieldWidths[i];
            }

            // extract entries
            _entries = new List<PdfCrossReference>();
            for (int i = 0; i < stream.Length; i += entryWidth)
            {
                // update object numbering where required
                if (sections != null)
                {
                    int sectionStartNo = sections[sectionsInx];
                    int sectionLen = sections[sectionsInx + 1];
                    if (objNo - sectionStartNo >= sectionLen)
                    {
                        sectionsInx += 2;
                        objNo = sections[sectionsInx];
                    }
                }

                int type = getFieldFns[0](i) ?? 1;
                if (type < 0 || type > 2)
                    throw new ArgumentException("Xref table first entry must be 0, 1, or 2");
                XrefEntryType xrefType = (XrefEntryType)type;

                if (xrefType == XrefEntryType.Used && sections == null)
                    throw new ArgumentException("/Index is required for Used type entries");

                // type 0 - obj no of next free, type 1 - file offset, type 2 - object no of the stream (gen 0 assumed)
                int field2 = getFieldFns[1](i) ?? 0;
                // type 0 - gen no, type 1 - gen no (deft 0), type 2 - stream offset
                int field3 = getFieldFns[2](i) ?? 0;

                _entries.Add(new PdfCrossReference
                {
                    EntryType = xrefType,
                    ObjectNumber = objNo++,
                    StreamObjectNumber = xrefType == XrefEntryType.InStream ? field2 : 0,
                    Generation = xrefType == XrefEntryType.InStream ? 0 : field3,
                    Offset = xrefType == XrefEntryType.Free ? 0 : (xrefType == XrefEntryType.Used ? field2 : field3)
                });
            }
        }

        private int[] GetFieldSizes()
        {
            var fieldSizeArray = Dict["/W"] as PdfArray; // required field, so exception on failure is fine.
            if (fieldSizeArray.Count != 3)
                throw new ArgumentException("Field size array /W must contain three values.");
            return fieldSizeArray.Select(item => (int)(item as PdfNumeric)).ToArray();
        }

        private Func<int, int?> CreateDecodeNext(byte[] stream, int fieldWidth, int fieldOffset)
        {
            byte[] buf = fieldWidth == 0 ? null : new byte[fieldWidth];
            return delegate (int entryStartInx)
            {
                if (fieldWidth == 0)
                    return null;
                Array.Copy(stream, entryStartInx + fieldOffset, buf, 0, fieldWidth);
                return Util.ByteConversions.ReadBigEndianInt(buf, 0, buf.Length);
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Types.Basic
{
    public class PdfDictionary : Dictionary<PdfName, IPdfObject>, IPdfObject
    {
        public PdfDictionary() { }

        public PdfDictionary(PdfDictionary dict)
        {
            Copy(dict);
        }

        public T Get<T>(string key) where T : class, IPdfObject
        {
            if (!ContainsKey(key))
                return null;

            IPdfObject obj = this[key];

            if(obj is PdfReference)
                obj = (obj as PdfReference).GetTarget();

            return obj as T;
        }

        public void Copy(PdfDictionary dict)
        {
            foreach (var kvp in dict)
                this[kvp.Key] = kvp.Value;
        }

        protected T GetReferencedObject<T>(string name) where T : class, IPdfObject
        {
            // The idea is that (eventually) there would be .Add() methods (probably on, or using the PdfDoc) that would
            // create a new object in the document, a new reference to it, and assign the reference to a dictionary entry
            // For now we're just manipulating the objects we already have.
            if (!ContainsKey(name))
                throw new InvalidStructureException("Object does not have requested reference") { FieldName = name };

            var contents = this[name] as PdfReference;
            T obj = contents.GetTarget() as T;
            if (obj == null)
                throw new InvalidStructureException("Object does not have requested reference") { FieldName = name };

            return obj;
        }
    }
}

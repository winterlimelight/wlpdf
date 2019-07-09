using Wlpdf.Types.Basic;

namespace Wlpdf.Types.Object
{
    interface ITypedObject
    {
        PdfDictionary Dict { get; }
    }
}

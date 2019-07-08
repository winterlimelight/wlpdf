namespace Wlpdf.Reading
{
    internal enum TokenType
    {
        Whitespace = 1, MagicNumber, Null,
        Boolean, Integer, Real, NonObjectString, String, HexString, Name,
        Array, EndArray, Dict, EndDict,
        Stream, EndStream, Object, EndObject,
        Xref, Trailer, StartXref
    }

    internal class Token
    {
        public TokenType Type;
        public object Value;
        public int Offset;
    }
}

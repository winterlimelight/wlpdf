using System;
using System.Collections.Generic;
using System.Linq;

namespace Wlpdf.Reading
{
    public class Lexer
    {
        private byte[] _s;
        private int _at;
        private int _offset;

        internal Lexer SetSource(byte[] source)
        {
            _s = source;
            _at = _offset = 0;
            Current = null;
            return this;
        }

        internal Token Current { get; private set; }

        internal Token Next()
        {
            if(_at == 0 && IsText("%PDF-1."))
            {
                char v = (char)_s[_at++];
                if(!IsWhitespace())
                    throw new LexerException($"Incorrect document version.");
                return SetToken(TokenType.MagicNumber, $"1.{v}");
            }

            while(!IsEof())
            {
                if (IsWhitespace())
                { 
                    ConsumeWhitespace();
                    continue;
                }
                if (TryKeyword("true"))
                    return SetToken(TokenType.Boolean, true);
                if (TryKeyword("false"))
                    return SetToken(TokenType.Boolean, false);
                if (TryKeyword("obj"))
                    return SetToken(TokenType.Object, null);
                if (TryKeyword("endobj"))
                    return SetToken(TokenType.EndObject, null);
                if (IsText("stream\n") || IsText("stream\r\n"))
                    return SetToken(TokenType.Stream, null);
                if (TryKeyword("endstream"))
                    return SetToken(TokenType.EndStream, null);
                if (TryKeyword("xref"))
                    return SetToken(TokenType.Xref, null);
                if(TryKeyword("trailer"))
                    return SetToken(TokenType.Trailer, null);
                if (TryKeyword("null"))
                    return SetToken(TokenType.Null, null);

                char c = (char)_s[_at];
                if (c == '/')
                    return ConsumeName();
                if (c == '(')
                    return ConsumeString();
                if (c == '[')
                    return ConsumeFixedLength(1, TokenType.Array);
                if (c == ']')
                    return ConsumeFixedLength(1, TokenType.EndArray);
                if (c == '<')
                {
                    // peek ahead to look for <<
                    if ((char)_s[_at + 1] == '<')
                        return ConsumeFixedLength(2, TokenType.Dict);
                    else
                        return ConsumeHexString();
                }
                if (c == '>')
                {
                    // peek ahead to look for >>
                    if ((char)_s[_at + 1] == '>')
                        return ConsumeFixedLength(2, TokenType.EndDict);
                }
                if (IsNumeric(c))
                    return ConsumeNumber();
                if (char.IsLetter(c))
                    return ConsumeNonObjectString();
                else
                    throw new LexerException($"Unknown token at {_at}");
            }
            return null;
        }

        internal Token Peek()
        {
            return Peek(1)[0];
        }

        internal Token[] Peek(int depth)
        {
            // save state
            int at = _at;
            int offset = _offset;
            Token current = Current;

            // move on
            var tokens = new Token[depth];
            for (int i = 0; i < depth; i++)
            {
                Next();
                tokens[i] = Current;
            }

            // restore state
            _at = at;
            _offset = offset;
            Current = current;

            return tokens;
        }

        internal void SeekToXref()
        {
            // The last line of the file shall contain only the end-of-file marker %%EOF. The two preceding lines shall contain,
            // one per line and in order, the keyword startxref and the byte offset in the decoded stream from the beginning
            // of the file of the xref keyword in the last cross-reference section
            _at = _s.Length - 1;
            while (IsWhitespace()) _at--;

            int offset = _at - "%%EOF".Length;
            _at = offset + 1; // +1 as _at would have been before the first character
            if (!IsText("%%EOF"))
                throw new LexerException("%%EOF marker not located at the end of the file");

            _at = offset;
            while (IsWhitespace()) _at--;

            var chars = new System.Collections.Generic.List<char>();
            while (char.IsDigit((char)_s[_at])) {
                chars.Insert(0, (char)_s[_at]);
                _at--;
            }

            _offset = _at = int.Parse(new string(chars.ToArray()));
            Current = null;
        }

        internal void Seek(int offset)
        {
            if (offset < 0 || offset >= _s.Length)
                throw new LexerException($"Invalid seek offset {offset}");

            _offset = _at = offset;
            Current = null;
        }


        internal byte[] Take(int length)
        {
            byte[] arr = new byte[length];
            Array.Copy(_s, _at, arr, 0, length);

            _at += length;
            _offset = _at;
            Current = null;

            return arr;
        }

        private Token SetToken(TokenType t, object value)
        {
            Current = new Token() { Type = t, Offset = _offset, Value = value};
            _offset = _at;
            return Current;
        }

        private void ConsumeWhitespace()
        {
            while (!IsEof() && IsWhitespace())
            {
                _at++;
                _offset = _at;
            }
        }


        private void ConsumeStream()
        {
            // TODO - another while loop I'm guessing....
        }

        private Token ConsumeName()
        {
            char c = (char)_s[++_at];
            while (!IsWhitespace() && !IsDelimiter())
                c = (char)_s[++_at];
            // TODO - special handling for #

            string s = new string(System.Text.Encoding.ASCII.GetChars(_s, _offset, _at - _offset));
            return SetToken(TokenType.Name, s);
        }

        private Token ConsumeNumber()
        {
            char c = (char)_s[_at];
            while (IsNumeric(c))
                c = (char)_s[++_at];

            string s = new string(System.Text.Encoding.ASCII.GetChars(_s, _offset, _at - _offset));
            return SetToken(TokenType.Numeric, s.Contains(".") ? (object)double.Parse(s) : (object)int.Parse(s));
        }


        private Token ConsumeNonObjectString()
        {
            char c = (char)_s[_at];
            while (char.IsLetter(c))
                c = (char)_s[++_at];

            return SetToken(TokenType.NonObjectString, new string(System.Text.Encoding.ASCII.GetChars(_s, _offset, _at - _offset)));
        }

        private Token ConsumeString()
        {
            int parenCount = 1; // allows for balanced parentheses within the string
            var bytes = new List<byte>();

            char c = (char)_s[++_at];
            bool isUnicode = _s[_at] == 254 && _s[_at + 1] == 255; // look for by BOM (254, 255)

            while (true)
            {
                if (c == '\\')
                {
                    c = (char)_s[++_at];
                    switch (c)
                    {
                        case 'n': c = '\n'; break;
                        case 'r': c = '\r'; break;
                        case 't': c = '\t'; break;
                        case 'b': c = '\b'; break;
                        case 'f': c = '\f'; break;
                        case '(':
                        case ')':
                        case '\\':
                            break; // drops the slash and adds an escaped char: ()\
                    }
                }
                else if (c == ')')
                {
                    parenCount--;
                    if (parenCount == 0)
                        break;
                }
                else if (c == '(')
                    parenCount++;

                bytes.Add((byte)c);
                c = (char)_s[++_at];

                if (_at >= _s.Length)
                    throw new LexerException("Unterminated string");
            }

            string s;
            if(isUnicode)
                s = new string(System.Text.Encoding.BigEndianUnicode.GetChars(bytes.ToArray()));
            else
                s = new string(System.Text.Encoding.ASCII.GetChars(bytes.ToArray())); // TODO should really be PdfDocEncoding which has some different chars in the upper 128
            
            _at++; // consume final paren
            return SetToken(TokenType.String, s);
        }

        private Token ConsumeHexString()
        {
            char c = (char)_s[++_at];
            var chars = new List<char>();
            while (c != '>')
            {
                if (IsWhitespace())
                    continue;
                if (!"0123456789ABCDEFabcdef".Contains(c))
                    throw new LexerException($"Unexpected character in hex string at {_offset}");

                chars.Add(c);
                c = (char)_s[++_at];
            }

            _at++; // consuming closing >
            return SetToken(TokenType.HexString, new string(chars.ToArray()));
        }

        private Token ConsumeFixedLength(int len, TokenType tt)
        {
            _at += len;
            return SetToken(tt, null);
        }

        private bool IsWhitespace()
        {
            byte b = _s[_at];
            return b == 0 || b == 9 || b == 10 || b == 12 || b == 13 || b == 32; // null, h-tab, line-feed, form-feed, carriage-return, space
        }

        private bool IsNumeric(char c)
        {
            return "0123456789.+-".Contains(c);
        }

        private bool IsDelimiter()
        {
            char c = (char)_s[_at];
            return "()<>[]{}/%".Contains(c);
        }

        private bool IsEof()
        {
            return _at >= _s.Length;
        }

        // consumes keyword if matches
        private bool TryKeyword(string str)
        {
            if (!IsText(str))
                return false;

            // peek whitespace - do not consume
            if(!IsWhitespace())
                return false;

            return true;
        }

        // consumes text if matches
        private bool IsText(string str)
        {
            int at = _at;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str);

            for (int i = 0; i < bytes.Length; i++)
            {
                if (_s[_at++] != bytes[i])
                {
                    _at = at;
                    return false;
                }
            }
            return true;
        }
    }
}

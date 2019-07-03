using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Wlpdf.Reading;

namespace Wlpdf.Types
{
    /// <remarks>
    /// This allows access to an existing CMap. It does not allow anything to be added to the CMap because
    /// any glyphs not here are unlikely to be in the font stream as the whole point of this approach is it
    /// allows only a subset of the font to be included to stop files getting too large.
    /// </remarks>
    public class UnicodeCMap
    {
        private PdfStream _stream;
        private readonly Dictionary<int, int> _toCode = new Dictionary<int, int>(); // glyph index into font -> char-code

        private readonly Regex _reBeginbfrange = new Regex(@"\d+\sbeginbfrange\s");
        private readonly Regex _reEndbfrange = new Regex(@"\sendbfrange\s");

        internal UnicodeCMap(PdfStream stream)
        {
            _stream = stream;
            Parse(Encoding.ASCII.GetString(_stream.Stream));
        }

        internal bool ContainsGlyph(int glyph)
        {
            return _toCode.ContainsKey(glyph);
        }

        internal int FromGlyph(int glyph)
        {
            return _toCode[glyph];
        }

        private void Parse(string content)
        {
            Match matchStart = _reBeginbfrange.Match(content);
            Match matchEnd = _reEndbfrange.Match(content);
            if (matchStart.Success && matchEnd.Success)
            {
                int start = matchStart.Index + matchStart.Length;
                ReadRange(content.Substring(start, matchEnd.Index - start));
            }
        }

        private void ReadRange(string subset)
        {
            var lexer = new Lexer();
            lexer.SetSource(Encoding.ASCII.GetBytes(subset));

            while (lexer.Next() != null)
            {
                Expect(lexer, TokenType.HexString);
                PdfHexString from = new PdfHexString { Text = (string)lexer.Current.Value };

                lexer.Next();
                Expect(lexer, TokenType.HexString);
                PdfHexString to = new PdfHexString { Text = (string)lexer.Current.Value };

                lexer.Next();
                if (Accept(lexer, TokenType.HexString))
                {
                    int offset = (new PdfHexString { Text = (string)lexer.Current.Value }).ToInt;
                    for (int i = from.ToInt; i <= to.ToInt; i++)
                        _toCode[i + offset] = i;
                }
                else if (Accept(lexer, TokenType.Array))
                {
                    int index = from.ToInt;
                    while(true)
                    {
                        lexer.Next();
                        if (Accept(lexer, TokenType.EndArray))
                            break;
                        else if (Accept(lexer, TokenType.HexString))
                        {
                            int glyph = (new PdfHexString { Text = (string)lexer.Current.Value }).ToInt;
                            _toCode[glyph] = index++;
                        }
                        else
                            throw new ParserException($"Unable to parse ToUnicode CMap. Expected HexString or End Array at {lexer?.Current.Type}");
                    }
                }
                else
                    throw new ParserException($"Unable to parse ToUnicode CMap. Expected HexString or Array at {lexer?.Current.Type}");
            }
        }

        private void Expect(Lexer lexer, TokenType t)
        {
            if (lexer.Current == null || lexer.Current.Type != t)
                throw new ParserException($"Unable to parse ToUnicode CMap. Expected {t} at {lexer?.Current.Type}");
        }

        private bool Accept(Lexer lexer, TokenType t)
        {
            if (lexer.Current == null)
                throw new ParserException($"Unexpected EOF in ToUnicode CMap while testing for {t}");
            return lexer.Current.Type == t;
        }
    }
}

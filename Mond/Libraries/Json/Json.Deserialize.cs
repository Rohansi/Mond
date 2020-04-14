using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mond.Binding;

namespace Mond.Libraries.Json
{
    internal partial class JsonModule
    {
        private const string DeserializePrefix = "Json.deserialize: ";

        [MondFunction]
        public static MondValue Deserialize(string text)
        {
            text = text.Trim();

            if (string.IsNullOrEmpty(text))
                return MondValue.Undefined;

            var parser = new Parser(text);

            var value = parser.ParseValue();
            parser.Require(TokenType.End);

            return value;
        }

        private class Parser
        {
            private readonly Lexer _lexer;
            private readonly List<Token> _read;

            public Parser(string text)
            {
                _lexer = new Lexer(text);
                _read = new List<Token>(4);
            }

            public MondValue ParseValue()
            {
                var token = Take();

                switch (token.Type)
                {
                    case TokenType.True:
                        return MondValue.True;

                    case TokenType.False:
                        return MondValue.False;

                    case TokenType.Null:
                        return MondValue.Null;

                    case TokenType.String:
                        return MondValue.String(token.Value);

                    case TokenType.Number:
                        if (!double.TryParse(token.Value, out var number))
                            throw new MondRuntimeException("Json.deserialize: invalid number '{0}'", token.Value);

                        return MondValue.Number(number);

                    case TokenType.ObjectStart:
                        return ParseObject();

                    case TokenType.ArrayStart:
                        return ParseArray();

                    default:
                        throw new MondRuntimeException("Json.deserialize: expected Value but got {0}", token.Type);
                }
            }

            private MondValue ParseObject()
            {
                var obj = MondValue.Object();
                var first = true;

                while (!Match(TokenType.ObjectEnd))
                {
                    if (first)
                        first = false;
                    else
                        Require(TokenType.Comma);

                    var key = Require(TokenType.String);

                    Require(TokenType.Colon);

                    var value = ParseValue();

                    obj[key.Value] = value;
                }

                Require(TokenType.ObjectEnd);
                return obj;
            }

            private MondValue ParseArray()
            {
                var arr = MondValue.Array();

                if (Match(TokenType.ArrayEnd))
                {
                    Take();
                    return arr;
                }

                arr.AsList.Add(ParseValue());

                while (!Match(TokenType.ArrayEnd))
                {
                    Require(TokenType.Comma);
                    arr.AsList.Add(ParseValue());
                }

                Require(TokenType.ArrayEnd);
                return arr;
            }

            public Token Require(TokenType type)
            {
                var token = Take();

                if (token.Type != type)
                    throw new MondRuntimeException("Json.deserialize: expected {0} but got {1}", type, token.Type);

                return token;
            }

            private bool Match(TokenType type)
            {
                return Peek().Type == type;
            }

            private Token Take()
            {
                Peek();

                var result = _read[0];
                _read.RemoveAt(0);
                return result;
            }

            private Token Peek(int distance = 0)
            {
                if (distance < 0)
                    throw new ArgumentOutOfRangeException(nameof(distance), "distance can't be negative");

                while (_read.Count <= distance)
                {
                    var token = _lexer.MoveNext() ? _lexer.Current : new Token(TokenType.End);

                    _read.Add(token);
                }

                return _read[distance];
            }
        }

        private enum TokenType
        {
            ObjectStart,
            ObjectEnd,

            ArrayStart,
            ArrayEnd,

            Colon,
            Comma,

            True,
            False,
            Null,

            String, 
            Number,

            End
        }

        private struct Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            public Token(TokenType type, string value = null)
            {
                Type = type;
                Value = value;
            }
        }

        private class Lexer : IEnumerator<Token>
        {
            private readonly string _text;

            private int _position;

            public Lexer(string text)
            {
                _text = text;
                _position = 0;
            }

            public Token Current { get; private set; }

            public bool MoveNext()
            {
                if (SkipWhiteSpace())
                    return false;

                var ch = TakeChar();

                switch (ch)
                {
                    case '{':
                        Current = new Token(TokenType.ObjectStart);
                        return true;

                    case '}':
                        Current = new Token(TokenType.ObjectEnd);
                        return true;

                    case '[':
                        Current = new Token(TokenType.ArrayStart);
                        return true;

                    case ']':
                        Current = new Token(TokenType.ArrayEnd);
                        return true;

                    case ':':
                        Current = new Token(TokenType.Colon);
                        return true;

                    case ',':
                        Current = new Token(TokenType.Comma);
                        return true;

                    case 't':
                        if (!IncrementIfNext("rue"))
                            throw UnexpectedChar();

                        Current = new Token(TokenType.True);
                        return true;

                    case 'f':
                        if (!IncrementIfNext("alse"))
                            throw UnexpectedChar();

                        Current = new Token(TokenType.False);
                        return true;

                    case 'n':
                        if (!IncrementIfNext("ull"))
                            throw UnexpectedChar();

                        Current = new Token(TokenType.Null);
                        return true;

                    case '"':
                        var sb = new StringBuilder();
                        var stringStart = _position - 1;

                        while (true)
                        {
                            if (_position >= _text.Length)
                                throw new MondRuntimeException(DeserializePrefix + "unterminated string starting at position {0}",
                                    stringStart);

                            ch = _text[_position++];

                            if (ch == '"')
                                break;

                            if (ch != '\\')
                            {
                                sb.Append(ch);
                                continue;
                            }

                            ch = TakeChar();

                            switch (ch)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    sb.Append(ch);
                                    break;

                                case 'b':
                                    sb.Append('\b');
                                    break;

                                case 'f':
                                    sb.Append('\f');
                                    break;

                                case 'n':
                                    sb.Append('\n');
                                    break;

                                case 'r':
                                    sb.Append('\r');
                                    break;

                                case 't':
                                    sb.Append('\t');
                                    break;

                                case 'u':
                                    var digits = "" + TakeChar() + TakeChar() + TakeChar() + TakeChar();

                                    if (!short.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
                                        goto default;

                                    sb.Append((char)value);
                                    continue;

                                default:
                                    throw new MondRuntimeException(DeserializePrefix + "invalid escape sequence '{0}' at position {1}",
                                        ch, _position - 1);
                            }
                        }

                        Current = new Token(TokenType.String, sb.ToString());
                        return true;

                    case '-':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        var start = _position - 1;

                        if (ch == '-')
                        {
                            // - must be followed by a digit
                            if (!char.IsDigit(TakeChar()))
                                goto default;
                        }

                        var hasDecimal = false;
                        var hasExp = false;

                        while (_position < _text.Length)
                        {
                            ch = _text[_position++];

                            if (ch == '.' && !hasDecimal && !hasExp)
                            {
                                hasDecimal = true;
                                continue;
                            }

                            if (ch == 'e' || ch == 'E' && !hasExp)
                            {
                                hasExp = true;

                                ch = TakeChar();

                                // e must be followed by a digit or +/-
                                if (char.IsDigit(ch))
                                    continue;

                                if (ch != '+' && ch != '-')
                                    goto default;

                                // +/- must be followed by a digit
                                if (!char.IsDigit(TakeChar()))
                                    goto default;

                                continue;
                            }

                            if (char.IsDigit(ch))
                                continue;

                            _position--;
                            break;
                        }

                        var numberStr = _text.Substring(start, _position - start);
                        Current = new Token(TokenType.Number, numberStr);
                        return true;

                    default:
                        throw UnexpectedChar();
                }
            }

            private char TakeChar()
            {
                if (_position >= _text.Length)
                    throw EndOfString();

                return _text[_position++];
            }

            private bool IncrementIfNext(string value)
            {
                if (!IsNext(value))
                    return false;

                _position += value.Length;
                return true;
            }

            private bool IsNext(string value)
            {
                var j = _position;
                for (var i = 0; i < value.Length; i++, j++)
                {
                    if (j >= _text.Length)
                        return false;

                    if (value[i] != _text[j])
                        return false;
                }

                return true;
            }

            private bool SkipWhiteSpace()
            {
                while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
                {
                    _position++;
                }

                return _position >= _text.Length;
            }

            private static Exception EndOfString()
            {
                return new MondRuntimeException(DeserializePrefix + "unexpected end of string");
            }

            private Exception UnexpectedChar()
            {
                return new MondRuntimeException(DeserializePrefix + "unexpected character '{0}' at position {1}",
                    _text[_position - 1], _position - 1);
            }

            public void Reset()
            {
                _position = 0;
            }

            public void Dispose() { }

            object IEnumerator.Current => Current;
        }
    }
}

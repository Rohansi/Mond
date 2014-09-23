using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Mond.Compiler
{
    partial class Lexer : IEnumerable<Token>
    {
        private readonly string _fileName;
        private readonly IEnumerable<char> _sourceEnumerable;

        private IEnumerator<char> _source;
        private int _length;
        private List<char> _read;

        private int _index;
        private int _currentLine;
        private int _startLine;

        public Lexer(IEnumerable<char> source, string fileName = null)
        {
            _fileName = fileName;
            _sourceEnumerable = source;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            _length = int.MaxValue;
            _source = _sourceEnumerable.GetEnumerator();
            _read = new List<char>(16);

            _index = 0;
            _currentLine = 1;

            while (_index < _length)
            {
                SkipWhiteSpace();

                if (SkipComments())
                    continue;

                if (_index >= _length)
                    break;

                _startLine = _currentLine;

                var ch = PeekChar();
                Token token;

                if (!TryLexOperator(ch, out token) &&
                    !TryLexString(ch, out token) &&
                    !TryLexWord(ch, out token) &&
                    !TryLexNumber(ch, out token))
                {
                    throw new MondCompilerException(_fileName, _currentLine, CompilerError.UnexpectedCharacter, ch);
                }

                yield return token;
            }

            while (true)
                yield return new Token(_fileName, _currentLine, TokenType.Eof, null);
        }

        private bool TryLexOperator(char ch, out Token token)
        {
            var opList = _operators.Lookup(ch);
            if (opList != null)
            {
                var op = opList.FirstOrDefault(o => TakeIfNext(o.Item1));

                if (op != null)
                {
                    token = new Token(_fileName, _currentLine, op.Item2, op.Item1);
                    return true;
                }
            }

            token = null;
            return false;
        }

        private bool TryLexString(char ch, out Token token)
        {
            if (ch == '\"' || ch == '\'')
            {
                TakeChar();

                var stringTerminator = ch;
                var stringContentsBuilder = new StringBuilder();

                while (true)
                {
                    if (_index >= _length)
                        throw new MondCompilerException(_fileName, _startLine, CompilerError.UnterminatedString);

                    ch = TakeChar();

                    if (ch == stringTerminator)
                        break;

                    switch (ch)
                    {
                        case '\\':
                            ch = TakeChar();

                            if (_index >= _length)
                                throw new MondCompilerException(_fileName, _currentLine, CompilerError.UnexpectedEofString);

                            switch (ch)
                            {
                                case '\\':
                                    stringContentsBuilder.Append('\\');
                                    break;

                                case '/':
                                    stringContentsBuilder.Append('/');
                                    break;

                                case '"':
                                    stringContentsBuilder.Append('"');
                                    break;

                                case '\'':
                                    stringContentsBuilder.Append('\'');
                                    break;

                                case 'b':
                                    stringContentsBuilder.Append('\b');
                                    break;

                                case 'f':
                                    stringContentsBuilder.Append('\f');
                                    break;

                                case 'n':
                                    stringContentsBuilder.Append('\n');
                                    break;

                                case 'r':
                                    stringContentsBuilder.Append('\r');
                                    break;

                                case 't':
                                    stringContentsBuilder.Append('\t');
                                    break;

                                case 'u':
                                    var i = 0;
                                    var hex = TakeWhile(c => ++i <= 4);
                                    short hexValue;

                                    if (!short.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexValue))
                                        throw new MondCompilerException(_fileName, _currentLine, CompilerError.InvalidEscapeSequence, ch + hex);

                                    stringContentsBuilder.Append((char)hexValue);
                                    break;

                                default:
                                    throw new MondCompilerException(_fileName, _currentLine, CompilerError.InvalidEscapeSequence, ch);
                            }

                            break;

                        default:
                            stringContentsBuilder.Append(ch);
                            break;
                    }
                }

                var stringContents = stringContentsBuilder.ToString();
                token = new Token(_fileName, _currentLine, TokenType.String, stringContents);
                return true;
            }

            token = null;
            return false;
        }

        private bool TryLexWord(char ch, out Token token)
        {
            if (char.IsLetter(ch) || ch == '_')
            {
                var wordContents = TakeWhile(c => char.IsLetterOrDigit(c) || c == '_');
                TokenType keywordType;
                var isKeyword = _keywords.TryGetValue(wordContents, out keywordType);

                token = new Token(_fileName, _currentLine, isKeyword ? keywordType : TokenType.Identifier, wordContents);
                return true;
            }

            token = null;
            return false;
        }

        private bool TryLexNumber(char ch, out Token token)
        {
            if (char.IsDigit(ch))
            {
                var format = NumberFormat.Decimal;
                var hasDecimal = false;
                var hasExp = false;
                var justTake = false;

                if (ch == '0')
                {
                    var nextChar = PeekChar(1);

                    if (nextChar == 'x' || nextChar == 'X')
                        format = NumberFormat.Hexadecimal;

                    if (nextChar == 'b' || nextChar == 'B')
                        format = NumberFormat.Binary;

                    if (format != NumberFormat.Decimal)
                    {
                        TakeChar(); // '0'
                        TakeChar(); // 'x' or 'b'
                    }
                }

                Func<char, bool> isDigit = c => char.IsDigit(c) || (format == NumberFormat.Hexadecimal && _hexChars.Contains(c));

                var numberContents = TakeWhile(c =>
                {
                    if (justTake)
                    {
                        justTake = false;
                        return true;
                    }

                    if (c == '_' && isDigit(PeekChar(1)))
                    {
                        TakeChar();
                        return true;
                    }

                    if (format == NumberFormat.Decimal)
                    {
                        if (c == '.' && !hasDecimal)
                        {
                            hasDecimal = true;
                            return isDigit(PeekChar(1));
                        }

                        if ((c == 'e' || c == 'E') && !hasExp)
                        {
                            var next = PeekChar(1);
                            if (next == '+' || next == '-')
                                justTake = true;

                            hasExp = true;
                            return true;
                        }
                    }

                    return isDigit(c);
                });

                double number;
                if (!TryParseNumber(numberContents, format, out number))
                    throw new MondCompilerException(_fileName, _currentLine, CompilerError.InvalidNumber, format.ToString().ToLower(), numberContents);

                token =  new Token(_fileName, _currentLine, TokenType.Number, number.ToString("G", CultureInfo.InvariantCulture));
                return true;
            }

            token = null;
            return false;
        }

        private bool SkipComments()
        {
            // single line comment
            if (TakeIfNext("//"))
            {
                while (!IsNext("\n"))
                {
                    TakeChar();
                }

                return true;
            }

            // multi line comment
            if (TakeIfNext("/*"))
            {
                var depth = 1;

                while (_index < _length && depth > 0)
                {
                    if (TakeIfNext("/*"))
                    {
                        depth++;
                        continue;
                    }

                    if (TakeIfNext("*/"))
                    {
                        depth--;
                        continue;
                    }

                    TakeChar();
                }

                return true;
            }

            return false;
        }

        private void SkipWhiteSpace()
        {
            while (_index < _length)
            {
                var ch = PeekChar();

                if (!char.IsWhiteSpace(ch))
                    break;

                TakeChar();
            }
        }

        private bool TakeIfNext(string value)
        {
            if (!IsNext(value))
                return false;

            for (var i = 0; i < value.Length; i++)
                TakeChar();

            return true;
        }

        private bool IsNext(string value)
        {
            if (_index + value.Length > _length)
                return false;

            return !value.Where((t, i) => PeekChar(i) != t).Any();
        }

        private string TakeWhile(Func<char, bool> condition)
        {
            var sb = new StringBuilder();

            while (_index < _length)
            {
                var ch = PeekChar();

                if (!condition(ch))
                    break;

                sb.Append(TakeChar());
            }

            return sb.ToString();
        }

        public char TakeChar()
        {
            PeekChar();

            var result = _read[0];
            _read.RemoveAt(0);
            _index++;

            if (result == '\n')
                _currentLine++;

            return result;
        }

        public char PeekChar(int distance = 0)
        {
            if (distance < 0)
                throw new ArgumentOutOfRangeException("distance", "distance can't be negative");

            while (_read.Count <= distance)
            {
                var success = _source.MoveNext();
                _read.Add(success ? _source.Current : '\0');

                if (!success)
                    _length = _index + _read.Count - 1;
            }

            return _read[distance];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        enum NumberFormat
        {
            Decimal, Hexadecimal, Binary
        }

        private bool TryParseNumber(string value, NumberFormat format, out double result)
        {
            int integralNumber;

            switch (format)
            {
                case NumberFormat.Decimal:
                    double floatNumber;
                    if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out floatNumber))
                    {
                        result = floatNumber;
                        return true;
                    }
                    break;

                case NumberFormat.Hexadecimal:
                    if (TryParseBase(value, 16, out integralNumber))
                    {
                        result = integralNumber;
                        return true;
                    }
                    break;

                case NumberFormat.Binary:
                    if (TryParseBase(value, 2, out integralNumber))
                    {
                        result = integralNumber;
                        return true;
                    }
                    break;

                default:
                    throw new MondCompilerException(_fileName, _currentLine, "Unsupported NumberFormat");
            }

            result = 0;
            return false;
        }

        private static bool TryParseBase(string value, int fromBase, out int result)
        {
            try
            {
                result = Convert.ToInt32(value, fromBase);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
    }
}

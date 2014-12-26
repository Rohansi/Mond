using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mond.Compiler.Parselets;

namespace Mond.Compiler
{
    partial class Lexer : IEnumerable<Token>
    {
        private struct Position
        {
            public readonly int Line;
            public readonly int Column;

            public Position(int line, int column)
                : this()
            {
                Line = line;
                Column = column;
            }
        }

        private readonly string _fileName;
        private readonly IEnumerable<char> _sourceEnumerable;

        private IEnumerator<char> _source;
        private int _length;
        private List<char> _read;

        private int _index;
        private int _currentLine;
        private int _currentColumn;
        private Stack<Position> _positions;

        public bool AtEof { get { return _index >= _length; } }

        public Lexer(IEnumerable<char> source, string fileName = null)
        {
            _fileName = fileName;
            _sourceEnumerable = source;
            _positions = new Stack<Position>();
        }

        public IEnumerator<Token> GetEnumerator()
        {
            _length = int.MaxValue;
            _source = _sourceEnumerable.GetEnumerator();
            _read = new List<char>(16);

            _index = 0;
            _currentLine = 1;
            _currentColumn = 1;

            while (!AtEof)
            {
                SkipWhiteSpace();

                if (SkipComments())
                    continue;

                if (AtEof)
                    break;

                var ch = PeekChar();
                Token token;

                if (!TryLexOperator(ch, out token) &&
                    !TryLexString(ch, out token) &&
                    !TryLexWord(ch, out token) &&
                    !TryLexNumber(ch, out token))
                {
                    throw new MondCompilerException(_fileName, _currentLine, _currentColumn, CompilerError.UnexpectedCharacter, ch);
                }

                yield return token;
            }

            while (true)
                yield return new Token(_fileName, _currentLine, _currentColumn, TokenType.Eof, null);
        }

        private bool TryLexOperator(char ch, out Token token)
        {
            var opList = _operators.Lookup(ch);
            if (opList != null)
            {
                MarkPosition();
                var op = opList.FirstOrDefault(o => TakeIfNext(o.Item1));

                if (op != null)
                {
                    var start = _positions.Pop();
                    token = new Token(_fileName, start.Line, start.Column, op.Item2, op.Item1);
                    return true;
                }

                _positions.Pop();
            }

            token = null;
            return false;
        }

        private bool TryLexString(char ch, out Token token)
        {
            if (ch == '\"' || ch == '\'')
            {
                MarkPosition();
                TakeChar();

                Position start;
                var stringTerminator = ch;
                var stringContentsBuilder = new StringBuilder();

                while (true)
                {
                    if (AtEof)
                    {
                        start = _positions.Peek();
                        throw new MondCompilerException(_fileName, start.Line, start.Column, CompilerError.UnterminatedString);
                    }

                    ch = TakeChar();

                    if (ch == stringTerminator)
                        break;

                    if (ch != '\\')
                    {
                        stringContentsBuilder.Append(ch);
                        continue;
                    }

                    MarkPosition();
                    ch = TakeChar();

                    if (AtEof)
                    {
                        start = _positions.Peek();
                        throw new MondCompilerException(_fileName, start.Line, start.Column, CompilerError.UnexpectedEofString);
                    }

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
                            {
                                start = _positions.Peek();
                                throw new MondCompilerException(_fileName, start.Line, start.Column, CompilerError.InvalidEscapeSequence, ch + hex);
                            }

                            stringContentsBuilder.Append((char)hexValue);
                            break;

                        default:
                            start = _positions.Peek();
                            throw new MondCompilerException(_fileName, start.Line, start.Column, CompilerError.InvalidEscapeSequence, ch);
                    }

                    _positions.Pop();
                }

                var stringContents = stringContentsBuilder.ToString();
                start = _positions.Pop();
                token = new Token(_fileName, start.Line, start.Column, TokenType.String, stringContents);
                return true;
            }

            token = null;
            return false;
        }

        private bool TryLexWord(char ch, out Token token)
        {
            if (char.IsLetter(ch) || ch == '_')
            {
                MarkPosition();
                var wordContents = TakeWhile(c => char.IsLetterOrDigit(c) || c == '_');
                TokenType keywordType;
                var isKeyword = _keywords.TryGetValue(wordContents, out keywordType);

                var start = _positions.Pop();
                token = new Token(_fileName, start.Line, start.Column, isKeyword ? keywordType : TokenType.Identifier, wordContents);
                return true;
            }

            token = null;
            return false;
        }

        private bool TryLexNumber(char ch, out Token token)
        {
            if (char.IsDigit(ch))
            {
                MarkPosition();
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

                var start = _positions.Pop();
                token =  new Token(_fileName, start.Line, start.Column, TokenType.Number, numberContents, format);
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
                while (!AtEof && !IsNext("\n"))
                {
                    TakeChar();
                }

                return true;
            }

            // multi line comment
            if (TakeIfNext("/*"))
            {
                var depth = 1;
                MarkPosition();

                while (!AtEof && depth > 0)
                {
                    if (TakeIfNext("/*"))
                    {
                        MarkPosition();
                        depth++;
                        continue;
                    }

                    if (TakeIfNext("*/"))
                    {
                        _positions.Pop();
                        depth--;
                        continue;
                    }

                    TakeChar();
                }

                if (AtEof && depth > 0)
                {
                    var lastPosition = _positions.Peek();
                    throw new MondCompilerException(_fileName, lastPosition.Line, lastPosition.Column, CompilerError.UnexpectedEofComment);
                }

                _positions.Pop();

                return true;
            }

            return false;
        }

        private void SkipWhiteSpace()
        {
            while (!AtEof)
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

            while (!AtEof)
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
            PeekChar(1);

            var result = _read[0];
            _read.RemoveAt(0);

            if (result == '\r' && _read[0] != '\n')
                throw new MondCompilerException(_fileName, _currentLine, _currentColumn, CompilerError.CrMustBeFollowedByLf);

            if (result == '\n')
                AdvanceLine();

            _index++;
            _currentColumn++;

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

        private void AdvanceLine()
        {
            _currentLine++;
            _currentColumn = 0;
        }

        private void MarkPosition()
        {
            var position = new Position(_currentLine, _currentColumn);
            _positions.Push(position);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

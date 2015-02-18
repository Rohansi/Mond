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

        private readonly MondCompilerOptions _options;
        private readonly string _fileName;
        private readonly IEnumerable<char> _sourceEnumerable;
        private readonly StringBuilder _sourceCode;

        private IEnumerator<char> _source;
        private int _length;
        private List<char> _read;

        private int _index;
        private int _currentLine;
        private int _currentColumn;
        private Stack<Position> _positions;

        public Lexer(
            IEnumerable<char> source,
            string fileName = null,
            MondCompilerOptions options = null,
            bool buildSourceString = false)
        {
            _options = options;
            _fileName = fileName;
            _sourceEnumerable = source;
            _positions = new Stack<Position>();

            if (buildSourceString)
                _sourceCode = new StringBuilder(4096);
        }

        public string SourceCode
        {
            get
            {
                if (_sourceCode == null)
                    return null;

                var result = _sourceCode.ToString();
                _sourceCode.Clear();
                return result;
            }
        }

        public bool AtEof
        {
            get { return _index >= _length; }
        }

        public IEnumerator<Token> GetEnumerator()
        {
            _length = int.MaxValue;
            _source = _sourceEnumerable.GetEnumerator();
            _read = new List<char>(16);

            _index = 0;
            _currentLine = _options == null ? 1 : _options.FirstLineNumber;
            _currentColumn = 1;

            if (_sourceCode != null)
                _sourceCode.Clear();

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
            if (ch != '\"' && ch != '\'')
            {
                token = null;
                return false;
            }

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

        private bool TryLexWord(char ch, out Token token)
        {
            if (!char.IsLetter(ch) && ch != '_')
            {
                token = null;
                return false;
            }

            MarkPosition();
            var wordContents = TakeWhile(c => char.IsLetterOrDigit(c) || c == '_');
            TokenType keywordType;
            var isKeyword = _keywords.TryGetValue(wordContents, out keywordType);

            var start = _positions.Pop();
            token = new Token(_fileName, start.Line, start.Column, isKeyword ? keywordType : TokenType.Identifier, wordContents);
            return true;
        }

        private bool TryLexNumber(char ch, out Token token)
        {
            if (!char.IsDigit(ch))
            {
                token = null;
                return false;
            }

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
                    // 0x_ or 0b_ aren't allowed
                    if (PeekChar(2) == '_')
                    {
                        TakeChar(); // '0'

                        token = new Token(_fileName, _currentLine, _currentColumn, TokenType.Number, "0", format);
                        return true;
                    }

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

                if (c == '_')
                {
                    // _ must be followed by a digit
                    if (!isDigit(PeekChar(1)))
                        return false;

                    // skip _ so it takes the digit
                    TakeChar();
                    return true;
                }

                if (format == NumberFormat.Decimal)
                {
                    // only allowed one . and it cant be after e
                    if (c == '.' && !hasDecimal && !hasExp)
                    {
                        hasDecimal = true;

                        // . must be followed by a digit
                        return isDigit(PeekChar(1));
                    }

                    // only allowed one e
                    if ((c == 'e' || c == 'E') && !hasExp)
                    {
                        var next = PeekChar(1);

                        // e can be followed by +/-
                        if (next == '+' || next == '-')
                        {
                            // take it next time
                            justTake = true;
                        }

                        hasExp = true;
                        return true;
                    }
                }

                return isDigit(c);
            });

            var start = _positions.Pop();

            if (string.IsNullOrEmpty(numberContents))
                throw new MondCompilerException(_fileName, start.Line, start.Column, CompilerError.EmptyNumber, format.GetName());

            token =  new Token(_fileName, start.Line, start.Column, TokenType.Number, numberContents, format);
            return true;
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

            if (_sourceCode != null && result != '\0')
                _sourceCode.Append(result);

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

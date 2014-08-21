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
        private readonly IEnumerator<char> _source;
        private int _length;
        private List<char> _read;

        private readonly string _fileName;

        private int _index;
        private int _currentLine;

        public Lexer(string source, string fileName = null)
            : this(source, source == null ? -1 : source.Length, fileName)
        {

        }

        public Lexer(IEnumerable<char> source, int length, string fileName = null)
        {
            if (source == null || length <= 0)
                throw new ArgumentNullException("source");

            _source = source.GetEnumerator();
            _length = length;
            _read = new List<char>(16);

            _fileName = fileName;

            _index = 0;
            _currentLine = 1;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            while (_index < _length)
            {
                SkipWhiteSpace();

                if (_index >= _length)
                    break;

                // single line comment (discarded)
                if (TakeIfNext("//"))
                {
                    while (!IsNext("\n"))
                    {
                        TakeChar();
                    }

                    continue;
                }

                // multi line comment (discarded)
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

                    continue;
                }

                var startLine = _currentLine;
                var ch = PeekChar();

                // operators
                if (_punctuation.Contains(ch))
                {
                    var op = _operators.FirstOrDefault(o => TakeIfNext(o.Item1));

                    if (op != null)
                    {
                        yield return new Token(_fileName, _currentLine, op.Item2, op.Item1);
                        continue;
                    }
                }

                // string
                if (TakeIfNext('"') || TakeIfNext('\''))
                {
                    var stringTerminator = ch;
                    var stringContentsBuilder = new StringBuilder();

                    while (true)
                    {
                        if (_index >= _length)
                            throw new MondCompilerException(_fileName, startLine, CompilerError.UnterminatedString);

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

                                    case '"':
                                        stringContentsBuilder.Append('"');
                                        break;

                                    case '\'':
                                        stringContentsBuilder.Append('\'');
                                        break;

                                    case 'r':
                                        stringContentsBuilder.Append('\r');
                                        break;

                                    case 'n':
                                        stringContentsBuilder.Append('\n');
                                        break;

                                    case 't':
                                        stringContentsBuilder.Append('\t');
                                        break;

                                    // TODO: more escape sequences

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
                    yield return new Token(_fileName, _currentLine, TokenType.String, stringContents);
                    continue;
                }

                // keyword/word
                if (char.IsLetter(ch) || ch == '_')
                {
                    var wordContents = TakeWhile(c => char.IsLetterOrDigit(c) || c == '_');
                    TokenType keywordType;
                    var isKeyword = _keywords.TryGetValue(wordContents, out keywordType);

                    yield return new Token(_fileName, _currentLine, isKeyword ? keywordType : TokenType.Identifier, wordContents);
                    continue;
                }

                // number
                if (char.IsDigit(ch))
                {
                    var numberContents = TakeWhile(c => char.IsDigit(c) || (c == '.' && char.IsDigit(PeekChar(1))));

                    double number;
                    if (!double.TryParse(numberContents, NumberStyles.AllowDecimalPoint, null, out number))
                        throw new MondCompilerException(_fileName, _currentLine, CompilerError.InvalidNumber);

                    yield return new Token(_fileName, _currentLine, TokenType.Number, numberContents);
                    continue;
                }

                throw new MondCompilerException(_fileName, _currentLine, CompilerError.UnexpectedCharacter, ch);
            }

            while (true)
                yield return new Token(_fileName, _currentLine, TokenType.Eof, null);
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

        private bool TakeIfNext(char value)
        {
            if (PeekChar() != value)
                return false;

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
    }
}

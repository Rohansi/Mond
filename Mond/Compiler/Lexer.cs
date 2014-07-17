using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mond.Compiler
{
    class Lexer : IEnumerable<Token>
    {
        private readonly string _fileName;
        private readonly string _source;

        private HashSet<char> _punctuation;
        private List<Tuple<string, TokenType>> _operators;
        private Dictionary<string, TokenType> _keywords;

        public Lexer(string source, string fileName = null)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException("source");

            _fileName = fileName;
            _source = source;

            _operators = new List<Tuple<string, TokenType>>
            {
                Tuple.Create(";", TokenType.Semicolon),
                Tuple.Create(",", TokenType.Comma),
                Tuple.Create(".", TokenType.Dot),
                Tuple.Create("=", TokenType.Assign),

                Tuple.Create("(", TokenType.LeftParen),
                Tuple.Create(")", TokenType.RightParen),

                Tuple.Create("{", TokenType.LeftBrace),
                Tuple.Create("}", TokenType.RightBrace),

                Tuple.Create("[", TokenType.LeftSquare),
                Tuple.Create("]", TokenType.RightSquare),

                Tuple.Create("+", TokenType.Add),
                Tuple.Create("-", TokenType.Subtract),
                Tuple.Create("*", TokenType.Multiply),
                Tuple.Create("/", TokenType.Divide),
                Tuple.Create("%", TokenType.Modulo),
                Tuple.Create("++", TokenType.Increment),
                Tuple.Create("--", TokenType.Decrement),

                Tuple.Create("+=", TokenType.AddAssign),
                Tuple.Create("-=", TokenType.SubtractAssign),
                Tuple.Create("*=", TokenType.MultiplyAssign),
                Tuple.Create("/=", TokenType.DivideAssign),
                Tuple.Create("%=", TokenType.ModuloAssign),
                
                Tuple.Create("==", TokenType.EqualTo),
                Tuple.Create("!=", TokenType.NotEqualTo),
                Tuple.Create(">", TokenType.GreaterThan),
                Tuple.Create(">=", TokenType.GreaterThanOrEqual),
                Tuple.Create("<", TokenType.LessThan),
                Tuple.Create("<=", TokenType.LessThanOrEqual),
                Tuple.Create("!", TokenType.LogicalNot),
                Tuple.Create("&&", TokenType.LogicalAnd),
                Tuple.Create("||", TokenType.LogicalOr),

                Tuple.Create("?", TokenType.QuestionMark),
                Tuple.Create(":", TokenType.Colon),
                Tuple.Create("->", TokenType.Pointy)
            };

            _keywords = new Dictionary<string, TokenType>
            {
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "null", TokenType.Null },
                { "undefined", TokenType.Undefined },
                { "NaN", TokenType.NaN },
                { "Infinity", TokenType.Infinity },
                { "var", TokenType.Var },
                { "fun", TokenType.Fun },
                { "return", TokenType.Return },
                { "seq", TokenType.Seq },
                { "yield", TokenType.Yield },
                { "if", TokenType.If },
                { "else", TokenType.Else },
                { "for", TokenType.For },
                { "while", TokenType.While },
                { "do", TokenType.Do },
                { "break", TokenType.Break },
                { "continue", TokenType.Continue },
                { "switch", TokenType.Switch },
                { "case", TokenType.Case },
                { "default", TokenType.Default },
            };

            // longest operators need to be first
            _operators = _operators.OrderByDescending(o => o.Item1.Length).ToList();

            // punctuation characters trigger operator detection, this should
            // contain the first character of each operator
            _punctuation = new HashSet<char>();

            foreach (var ch in _operators.Select(t => t.Item1[0]))
            {
                _punctuation.Add(ch);
            }
        }

        public IEnumerator<Token> GetEnumerator()
        {
            var index = 0;
            var currentLine = 1;

            // Resharper disable AccessToModifiedClosure
            Action skipWhiteSpace = () =>
            {
                while (index < _source.Length && char.IsWhiteSpace(_source[index]))
                {
                    if (_source[index] == '\n')
                        currentLine++;

                    index++;
                }
            };

            Func<string, bool> isNext = str =>
            {
                if (index + str.Length > _source.Length)
                    return false;

                return _source.Substring(index, str.Length) == str;
            };

            Func<Func<char, bool>, string> takeWhile = cond =>
            {
                var start = index;
                while (index < _source.Length)
                {
                    if (!cond(_source[index]))
                        break;

                    index++;
                }

                return _source.Substring(start, index - start);
            };
            // Resharper enable AccessToModifiedClosure

            while (_source.Length > index) // swapped because resharper
            {
                skipWhiteSpace();

                if (index >= _source.Length)
                    break;

                // single line comment (discarded)
                if (index < _source.Length - 1 && _source.Substring(index, 2) == "//")
                {
                    while (index < _source.Length && _source[index] != '\n')
                    {
                        index++;
                    }

                    continue;
                }

                // multi line comment (discarded)
                if (index < _source.Length - 1 && _source.Substring(index, 2) == "/*")
                {
                    while (_source.Substring(index, 2) != "*/")
                    {
                        if (_source[index++] == '\n')
                            currentLine++;
                    }

                    index += 2;
                    continue;
                }

                var startLine = currentLine;
                var ch = _source[index];

                // operators
                if (_punctuation.Contains(ch))
                {
                    var op = _operators.FirstOrDefault(o => isNext(o.Item1));

                    if (op != null)
                    {
                        var opText = _source.Substring(index, op.Item1.Length);
                        yield return new Token(_fileName, currentLine, op.Item2, opText);
                        index += op.Item1.Length;
                        continue;
                    }
                }

                // string
                if (ch == '"' || ch == '\'')
                {
                    var stringTerminator = ch;
                    var stringContentsBuilder = new StringBuilder();

                    index++; // skip open quote

                    while (true)
                    {
                        if (index >= _source.Length)
                            throw new MondCompilerException(_fileName, startLine, CompilerError.UnterminatedString);

                        ch = _source[index];

                        if (ch == stringTerminator)
                            break;

                        switch (ch)
                        {
                            case '\n':
                                currentLine++;
                                goto default;

                            case '\\':
                                index++;
                                if (index >= _source.Length)
                                    throw new MondCompilerException(_fileName, currentLine, CompilerError.UnexpectedEofString);

                                ch = _source[index];

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

                                    case 'n':
                                        stringContentsBuilder.Append('\n');
                                        break;

                                    case 't':
                                        stringContentsBuilder.Append('\t');
                                        break;

                                    // TODO: more escape sequences

                                    default:
                                        throw new MondCompilerException(_fileName, currentLine, CompilerError.InvalidEscapeSequence, ch);
                                }

                                break;

                            default:
                                stringContentsBuilder.Append(ch);
                                break;
                        }

                        index++;
                    }

                    index++; // skip end quote

                    var stringContents = stringContentsBuilder.ToString();
                    yield return new Token(_fileName, currentLine, TokenType.String, stringContents);
                    continue;
                }

                // keyword/word
                if (char.IsLetter(ch) || ch == '_')
                {
                    var wordContents = takeWhile(c => char.IsLetterOrDigit(c) || c == '_');
                    TokenType keywordType;
                    var isKeyword = _keywords.TryGetValue(wordContents, out keywordType);

                    yield return new Token(_fileName, currentLine, isKeyword ? keywordType : TokenType.Identifier, wordContents);
                    continue;
                }

                // number
                if (char.IsDigit(ch))
                {
                    var numberContents = takeWhile(c => char.IsDigit(c) || c == '.');

                    double number;
                    if (!double.TryParse(numberContents, NumberStyles.AllowDecimalPoint, null, out number))
                        throw new MondCompilerException(_fileName, currentLine, CompilerError.InvalidNumber);

                    yield return new Token(_fileName, currentLine, TokenType.Number, numberContents);
                    continue;
                }

                throw new MondCompilerException(_fileName, currentLine, CompilerError.UnexpectedCharacter, ch);
            }

            while (true)
                yield return new Token(_fileName, currentLine, TokenType.Eof, null);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

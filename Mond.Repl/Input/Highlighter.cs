using System;
using System.Linq;

namespace Mond.Repl.Input
{
    public struct ColoredCharacter
    {
        public readonly char Character;
        public readonly ConsoleColor Color;

        public ColoredCharacter(char character, ConsoleColor color)
        {
            Character = character;
            Color = color;
        }
    }

    public partial class Highlighter
    {
        enum TokenState
        {
            None, MultiComment, String
        }

        enum NumberFormat
        {
            Decimal, Hexadecimal, Binary
        }

        private TokenState _state;
        private int _multiCommentDepth;
        private char _stringTerminator;

        public Highlighter Clone()
        {
            return new Highlighter
            {
                _state = _state,
                _multiCommentDepth = _multiCommentDepth,
                _stringTerminator = _stringTerminator
            };
        }

        public ColoredCharacter[] Highlight(string line)
        {
            var index = 0;
            var result = new ColoredCharacter[line.Length];

            while (index < line.Length)
            {
                switch (_state)
                {
                    case TokenState.None:
                        ColorNone(line, result, ref index);
                        break;

                    case TokenState.MultiComment:
                        ColorMultiComment(line, result, ref index);
                        break;

                    case TokenState.String:
                        ColorString(line, result, ref index);
                        break;
                }
            }

            return result;
        }

        private void ColorNone(string line, ColoredCharacter[] result, ref int index)
        {
            while (index < line.Length)
            {
                if (OutputIfNext(line, result, ref index, "//", CommentColor))
                {
                    for (var i = index; i < line.Length; i++, index++)
                    {
                        result[i] = new ColoredCharacter(line[i], CommentColor);
                    }

                    return;
                }

                if (OutputIfNext(line, result, ref index, "/*", CommentColor))
                {
                    _state = TokenState.MultiComment;
                    _multiCommentDepth = 1;
                    return;
                }

                var ch = line[index];

                if (OutputIfNext(line, result, ref index, "\"", StringColor) ||
                    OutputIfNext(line, result, ref index, "\'", StringColor))
                {
                    _state = TokenState.String;
                    _stringTerminator = ch;
                    return;
                }


                string[] operators;
                if (Operators.TryGetValue(ch, out operators))
                {
                    var idx = index;
                    var op = operators.FirstOrDefault(s => IsNext(line, idx, s));

                    if (op != null)
                    {
                        Output(result, ref index, op, OperatorColor);
                        continue;
                    }
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    var start = index;

                    while (index < line.Length && (char.IsLetterOrDigit(line[index]) || line[index] == '_'))
                    {
                        index++;
                    }

                    var word = line.Substring(start, index - start);
                    index = start;

                    if (Keywords.Contains(word))
                    {
                        Output(result, ref index, word, KeywordColor);
                        continue;
                    }

                    Output(result, ref index, word, IdentifierColor);
                    continue;
                }

                if (char.IsDigit(ch))
                {
                    var format = NumberFormat.Decimal;
                    var hasDecimal = false;
                    var hasExp = false;
                    var justTake = false;

                    if (ch == '0' && index + 1 < line.Length)
                    {
                        var nextChar = line[index + 1];

                        if (nextChar == 'x' || nextChar == 'X')
                            format = NumberFormat.Hexadecimal;

                        if (nextChar == 'b' || nextChar == 'B')
                            format = NumberFormat.Binary;

                        if (format != NumberFormat.Decimal)
                        {
                            if (index + 2 < line.Length && line[index + 2] == '_')
                            {
                                result[index++] = new ColoredCharacter('0', NumberColor);
                                continue;
                            }

                            result[index + 0] = new ColoredCharacter('0', NumberColor);
                            result[index + 1] = new ColoredCharacter(nextChar, NumberColor);
                            index += 2;
                        }
                    }

                    Func<char, bool> isDigit = c => char.IsDigit(c) || (format == NumberFormat.Hexadecimal && HexChars.Contains(c));

                    while (index < line.Length)
                    {
                        var c = line[index];

                        if (justTake)
                        {
                            justTake = false;
                            result[index++] = new ColoredCharacter(c, NumberColor);
                            continue;
                        }

                        if (c == '_' && (index + 1 < line.Length && isDigit(line[index + 1])))
                        {
                            result[index++] = new ColoredCharacter(c, NumberColor);
                            continue;
                        }

                        if (format == NumberFormat.Decimal)
                        {
                            if (c == '.' && !hasDecimal && !hasExp)
                            {
                                hasDecimal = true;

                                if (index + 1 >= line.Length || !isDigit(line[index + 1]))
                                    break;

                                result[index++] = new ColoredCharacter(c, NumberColor);
                                continue;
                            }

                            if ((c == 'e' || c == 'E') && !hasExp)
                            {
                                if (index + 1 < line.Length)
                                {
                                    var next = line[index + 1];
                                    if (next == '+' || next == '-')
                                        justTake = true;
                                }

                                hasExp = true;
                                result[index++] = new ColoredCharacter(c, NumberColor);
                                continue;
                            }
                        }

                        if (!isDigit(c))
                            break;

                        result[index++] = new ColoredCharacter(c, NumberColor);
                    }

                    continue;
                }

                result[index] = new ColoredCharacter(line[index], OtherColor);
                index++;
            }
        }

        private void ColorMultiComment(string line, ColoredCharacter[] result, ref int index)
        {
            while (_multiCommentDepth > 0)
            {
                if (index >= line.Length)
                    return;

                if (OutputIfNext(line, result, ref index, "/*", CommentColor))
                {
                    _multiCommentDepth++;
                    continue;
                }

                if (OutputIfNext(line, result, ref index, "*/", CommentColor))
                {
                    _multiCommentDepth--;
                    continue;
                }

                result[index] = new ColoredCharacter(line[index], CommentColor);
                index++;
            }

            _state = TokenState.None;
        }

        private void ColorString(string line, ColoredCharacter[] result, ref int index)
        {
            while (index < line.Length)
            {
                var ch = line[index];

                if (ch == '\\' && index + 1 < line.Length && line[index + 1] == _stringTerminator)
                {
                    result[index + 0] = new ColoredCharacter('\\', StringColor);
                    result[index + 1] = new ColoredCharacter(_stringTerminator, StringColor);
                    index += 2;
                    continue;
                }

                result[index] = new ColoredCharacter(ch, StringColor);
                index++;

                if (ch == _stringTerminator)
                {
                    _state = TokenState.None;
                    _stringTerminator = '\0';
                    return;
                }
            }
        }

        private static bool OutputIfNext(string line, ColoredCharacter[] result, ref int index, string value, ConsoleColor color)
        {
            var isNext = IsNext(line, index, value);

            if (isNext)
                Output(result, ref index, value, color);

            return isNext;
        }

        private static void Output(ColoredCharacter[] result, ref int index, string value, ConsoleColor color)
        {
            for (var i = 0; i < value.Length; i++, index++)
            {
                result[index] = new ColoredCharacter(value[i], color);
            }
        }

        private static bool IsNext(string line, int offset, string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var index = offset + i;
                if (index >= line.Length)
                    return false;

                if (line[index] != value[i])
                    return false;
            }

            return true;
        }
    }
}

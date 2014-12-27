using System;
using System.Collections.Generic;

namespace Mond.Repl.Input
{
    public static class Highlighted
    {
        private static int _position;
        private static List<int> _positions;

        static Highlighted()
        {
            _position = 0;
            _positions = new List<int>(8);
        }

        public static string ReadLine(ref Highlighter highlighter)
        {
            highlighter = highlighter ?? new Highlighter();

            _position = 0;
            _positions.Clear();

            var originalHighlighter = highlighter.Clone();

            var input = new List<char>(64);
            var caretIndex = 0;

            var previousColors = new ColoredCharacter[0];

            Func<Highlighter> redraw = () =>
            {
                var inputString = new string(input.ToArray());
                var newHighlighter = originalHighlighter.Clone();
                var currentColors = newHighlighter.Highlight(inputString);

                SavePosition();

                var length = Math.Max(currentColors.Length, previousColors.Length);

                int start = 0;
                for (; start < length; start++)
                {
                    if (start >= previousColors.Length || start >= currentColors.Length)
                        break;

                    var prev = previousColors[start];
                    var curr = currentColors[start];

                    if (prev.Character != curr.Character || prev.Color != curr.Color)
                        break;
                }

                Move(-_position + start);

                ConsoleColor? prevColor = null;

                for (var i = start; i < length; i++)
                {
                    var curr = new ColoredCharacter(' ', ConsoleColor.Gray);

                    if (i < currentColors.Length)
                        curr = currentColors[i];

                    if (!prevColor.HasValue || prevColor.Value != curr.Color)
                        Console.ForegroundColor = curr.Color;

                    prevColor = curr.Color;

                    Console.Write(curr.Character);
                    _position++;

                    if (Console.CursorLeft == Console.BufferWidth - 1 && Console.CursorTop == Console.BufferHeight - 1)
                    {
                        Console.WriteLine();
                        Move(-1);
                        _position++;
                    }
                }

                RestorePosition();

                previousColors = currentColors;
                return newHighlighter;
            };

            SavePosition();

            while (true)
            {
                var info = Console.ReadKey(true);

                switch (info.Key)
                {
                    case ConsoleKey.Enter:
                        RestorePosition(input.Count);
                        Console.ResetColor();
                        Console.WriteLine();
                        return new string(input.ToArray());

                    case ConsoleKey.LeftArrow:
                        if (caretIndex > 0)
                        {
                            caretIndex--;

                            Move(-1);
                        }
                        continue;

                    case ConsoleKey.RightArrow:
                        if (caretIndex < input.Count)
                        {
                            caretIndex++;

                            Move(1);
                        }
                        continue;

                    case ConsoleKey.Backspace:
                        if (caretIndex == 0)
                            continue;

                        caretIndex--;
                        input.RemoveAt(caretIndex);

                        highlighter = redraw();

                        Move(-1);
                        continue;

                    case ConsoleKey.Delete:
                        if (caretIndex == input.Count)
                            continue;

                        input.RemoveAt(caretIndex);

                        highlighter = redraw();
                        continue;

                    case ConsoleKey.Escape:
                        var count = input.Count;

                        if (count == 0)
                            continue;

                        input.Clear();
                        caretIndex = 0;

                        RestorePosition();
                        SavePosition();

                        highlighter = redraw();
                        continue;

                    default:
                        if (info.KeyChar == 0)
                            continue;

                        break;
                }

                if (caretIndex == input.Count)
                    input.Add(info.KeyChar);
                else
                    input.Insert(caretIndex, info.KeyChar);

                highlighter = redraw();
                Move(1);

                caretIndex++;
            }
        }

        private static void SavePosition()
        {
            _positions.Add(_position);
        }

        private static void RestorePosition(int offset = 0)
        {
            var index = _positions.Count - 1;
            var target = _positions[index] + offset;
            _positions.RemoveAt(index);

            Move(target - _position);
        }

        private static void Move(int offset)
        {
            var offsetLeft = offset % Console.BufferWidth;
            var offsetTop = offset / Console.BufferWidth;

            var left = Console.CursorLeft + offsetLeft;
            var top = Console.CursorTop + offsetTop;

            while (left < 0)
            {
                left += Console.BufferWidth;
                top--;
            }

            while (left >= Console.BufferWidth)
            {
                left -= Console.BufferWidth;
                top++;
            }

            _position += offset;
            Console.SetCursorPosition(left, top);
        }
    }
}

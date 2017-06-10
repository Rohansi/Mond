using System;
using System.Collections.Generic;

namespace Mond.Repl.Input
{
    public static class Highlighted
    {
        private static List<string> _history;
        private static int _historyIndex;
        private static string _current;
         
        private static int _position;
        private static List<int> _positions;

        private static List<char> _input;
        private static int _caretIndex;

        private static Highlighter _originalHighlighter;
        private static ColoredCharacter[] _previousColors;

        static Highlighted()
        {
            _history = new List<string>();

            _position = 0;
            _positions = new List<int>(8);

            _input = new List<char>(64);
        }

        public static string ReadLine(ref Highlighter highlighter)
        {
            highlighter = highlighter ?? new Highlighter();

            _historyIndex = -1;
            _current = "";

            _position = 0;
            _positions.Clear();

            _input.Clear();
            _caretIndex = 0;

            _originalHighlighter = highlighter.Clone();
            _previousColors = new ColoredCharacter[0];

            SavePosition();

            while (true)
            {
                var info = Console.ReadKey(true);

                switch (info.Key)
                {
                    case ConsoleKey.Enter:
                        var result = new string(_input.ToArray());

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            // add result to history
                            _history.Insert(0, result);
                            if (_history.Count > 50)
                                _history.RemoveAt(_history.Count - 1);
                        }

                        // restore console state
                        RestorePosition(_input.Count);
                        Console.ResetColor();
                        Console.WriteLine();
                        return result;

                    case ConsoleKey.UpArrow:
                        if (_historyIndex >= _history.Count -1)
                            continue;

                        if (_historyIndex == -1)
                            _current = new string(_input.ToArray());

                        _historyIndex++;
                        SetInput(_history[_historyIndex], out highlighter);
                        continue;

                    case ConsoleKey.DownArrow:
                        if (_historyIndex <= -1)
                            continue;

                        _historyIndex--;
                        var historyStr = _historyIndex == -1 ? _current : _history[_historyIndex];

                        SetInput(historyStr, out highlighter);
                        continue;

                    case ConsoleKey.LeftArrow:
                        if (_caretIndex == 0)
                            continue;

                        if (info.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            var diff = PreviousBoundary();
                            _caretIndex += diff;
                            Move(diff);

                            continue;
                        }

                        _caretIndex--;
                        Move(-1);

                        continue;

                    case ConsoleKey.RightArrow:
                        if (_caretIndex == _input.Count)
                            continue;

                        if (info.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            var diff = NextBoundary();
                            _caretIndex += diff;
                            Move(diff);

                            continue;
                        }

                        _caretIndex++;
                        Move(1);

                        continue;

                    case ConsoleKey.Home:
                        if (_caretIndex == 0)
                            continue;

                        Move(-_caretIndex);
                        _caretIndex = 0;
                        continue;

                    case ConsoleKey.End:
                        if (_caretIndex == _input.Count)
                            continue;

                        Move(_input.Count - _caretIndex);
                        _caretIndex = _input.Count;
                        continue;

                    case ConsoleKey.Backspace:
                        if (_caretIndex == 0)
                            continue;

                        if (info.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            var diff = PreviousBoundary();

                            _caretIndex += diff;
                            Move(diff);

                            _input.RemoveRange(_caretIndex, Math.Abs(diff));

                            highlighter = Redraw();
                            continue;
                        }

                        _caretIndex--;
                        _historyIndex = -1;
                        _input.RemoveAt(_caretIndex);

                        highlighter = Redraw();

                        Move(-1);
                        continue;

                    case ConsoleKey.Delete:
                        if (_caretIndex == _input.Count)
                            continue;

                        if (info.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            var diff = NextBoundary();

                            _input.RemoveRange(_caretIndex, diff);

                            highlighter = Redraw();
                            continue;
                        }

                        _historyIndex = -1;
                        _input.RemoveAt(_caretIndex);

                        highlighter = Redraw();
                        continue;

                    case ConsoleKey.Escape:
                        var count = _input.Count;

                        if (count == 0)
                            continue;

                        _input.Clear();
                        _caretIndex = 0;
                        _historyIndex = -1;

                        RestorePosition();
                        SavePosition();

                        highlighter = Redraw();
                        continue;

                    case ConsoleKey.Tab:
                        continue;

                    default:
                        if (info.KeyChar == 0)
                            continue;

                        break;
                }

                if (_caretIndex == _input.Count)
                    _input.Add(info.KeyChar);
                else
                    _input.Insert(_caretIndex, info.KeyChar);

                highlighter = Redraw();
                Move(1);

                _caretIndex++;
                _historyIndex = -1;
            }
        }

        private static int NextBoundary()
        {
            if (_caretIndex == _input.Count)
                return 0;

            var index = _caretIndex;

            while (index < _input.Count)
            {
                if (!char.IsLetterOrDigit(_input[index]))
                    break;

                index++;
            }

            return Math.Max(index - _caretIndex, 1);
        }

        private static int PreviousBoundary()
        {
            if (_caretIndex == 0)
                return 0;

            var index = _caretIndex - 1;

            while (index > 0)
            {
                if (!char.IsLetterOrDigit(_input[index]))
                {
                    index++;
                    break;
                }

                index--;
            }

            return Math.Min(index - _caretIndex, -1);
        }

        private static void SetInput(string value, out Highlighter highlighter)
        {
            _input.Clear();
            _input.AddRange(value);

            RestorePosition();
            SavePosition();

            highlighter = Redraw();

            _caretIndex = _input.Count;
            Move(_input.Count);
        }

        private static Highlighter Redraw()
        {
            var inputString = new string(_input.ToArray());
            var newHighlighter = _originalHighlighter.Clone();
            var currentColors = newHighlighter.Highlight(inputString);

            SavePosition();

            var length = Math.Max(currentColors.Length, _previousColors.Length);

            int start = 0;
            for (; start < length; start++)
            {
                if (start >= _previousColors.Length || start >= currentColors.Length)
                    break;

                var prev = _previousColors[start];
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

            _previousColors = currentColors;
            return newHighlighter;
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

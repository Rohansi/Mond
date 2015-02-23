using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Libraries;
using Mond.Repl.Input;

namespace Mond.Repl
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = args.FirstOrDefault(s => s.Length > 0 && s[0] != '-');

            if (fileName != null)
            {
                try
                {
                    using (var file = File.OpenRead(fileName))
                    using (var reader = new StreamReader(file))
                        ScriptMain(reader, fileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to open file '{0}':", fileName);
                    Console.WriteLine(e);
                }

                return;
            }

            if (Console.IsInputRedirected)
            {
                ScriptMain(Console.In, "stdin");
                return;
            }

            InteractiveMain(args);
        }

        static void ScriptMain(TextReader input, string fileName)
        {
            var state = new MondState();

            string source;

            try
            {
                source = input.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to read '{0}':", fileName);
                Console.WriteLine(e);
                return;
            }

            try
            {
                var result = state.Run(source, Path.GetFileName(fileName));

                if (result == MondValue.Undefined)
                    return;

                result.Serialize(Console.Out);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static Func<string> _readLine;
        private static Queue<char> _input;
        private static int _line;
        private static bool _first;
        private static Highlighter _highlighter;

        static void InteractiveMain(string[] args)
        {
            var useColoredInput = args.Any(s => s == "-c");

            if (useColoredInput)
            {
                _readLine = () => Highlighted.ReadLine(ref _highlighter);

                Console.CancelKeyPress += (sender, eventArgs) => Console.ResetColor();
            }
            else
            {
                _readLine = Console.ReadLine;
            }

            _input = new Queue<char>();
            _first = true;

            var libraries = new MondLibraryManager
            {
                new StandardLibraries()
            };

            var state = new MondState();
            var options = new MondCompilerOptions
            {
                MakeRootDeclarationsGlobal = true,
                UseImplicitGlobals = true
            };

            libraries.Load(state);

            while (true)
            {
                try
                {
                    options.FirstLineNumber = _line + 1;

                    foreach (var program in MondProgram.CompileStatements(ConsoleInput(), "stdin", options))
                    {
                        InteractiveRun(state, program);
                    }
                }
                catch (Exception e)
                {
                    PrintException(e);
                }
            }
        }

        static void InteractiveRun(MondState state, MondProgram program)
        {
            var result = state.Load(program);

            // get rid of leading whitespace
            while (_input.Count > 0 && char.IsWhiteSpace(_input.Peek()))
            {
                _input.Dequeue();
            }

            if (_input.Count != 0)
                return;

            _first = true;

            // ignore undefined return value, it's almost always useless
            if (result == MondValue.Undefined)
                return;

            if (result["moveNext"].Type == MondValueType.Function && result.IsEnumerable)
            {
                Console.WriteLine();

                foreach (var value in result.Enumerate(state).Take(25))
                {
                    value.Serialize(Console.Out);
                    Console.WriteLine();
                }

                if (state.Call(result["moveNext"]))
                    Console.WriteLine("...");

                Console.WriteLine();
            }
            else
            {
                var resultStr = result.Serialize();
                var multiline = resultStr.Contains("\n");

                if (multiline)
                {
                    Console.WriteLine();
                    Console.WriteLine(resultStr);
                    Console.WriteLine();
                }
                else
                {
                    var lineNumberLen = Math.Max(_line.ToString("G").Length, 3);
                    Console.WriteLine("{0}> {1}", new string('=', lineNumberLen), resultStr);
                }
            }
        }

        static IEnumerable<char> ConsoleInput()
        {
            while (true)
            {
                if (_input.Count == 0)
                {
                    Console.Write("{0,3:G}{1} ", ++_line, _first ? ">" : "|");

                    var line = _readLine();

                    if (line == null)
                        yield break;

                    if (_first && line.StartsWith("="))
                        line = "return " + line.Substring(1);

                    foreach (var c in line)
                    {
                        _input.Enqueue(c);
                    }

                    _input.Enqueue('\n');

                    if (!string.IsNullOrWhiteSpace(line))
                        _first = false;

                    continue;
                }

                yield return _input.Dequeue();
            }
        }

        static void PrintException(Exception e)
        {
            string message = e is MondException ? e.Message : e.ToString();
            string stackTrace = null;

            var runtimeException = e as MondRuntimeException;
            if (runtimeException != null)
                stackTrace = runtimeException.StackTrace;

            Console.WriteLine();
            Console.WriteLine(message);

            if (!message.EndsWith("\n"))
                Console.WriteLine();

            if (stackTrace != null)
                Console.WriteLine(stackTrace);

            _first = true;
            _input.Clear();

            _highlighter = null;
        }
    }
}

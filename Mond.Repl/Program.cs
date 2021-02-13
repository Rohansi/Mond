using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mond.RemoteDebugger;
using Mond.Repl.Input;

namespace Mond.Repl
{
    public class Program
    {
        private const int DebugPort = 1597;

        private static string[] _args;

        public static void Main(string[] args)
        {
            _args = args;

            if (HasFlag("-h", "--help"))
            {
                var mondVersion = typeof(MondState).GetTypeInfo().Assembly.GetName().Version;

                Console.WriteLine($"Mond REPL v{mondVersion.ToString(3)}");
                Console.WriteLine();
                Console.WriteLine($"Usage: Mond.Repl.exe [flags] [filename]");
                Console.WriteLine();
                Console.WriteLine($"-h, --help    Show REPL help");
                Console.WriteLine($"--no-color    Disable syntax highlighting");
                Console.WriteLine($"--debug       Start debugging on port {DebugPort}");
                Console.WriteLine($"--wait        When debugging, pauses at the start of the script");

                return;
            }

            var fileName = args.FirstOrDefault(s => s.Length > 0 && s[0] != '-');

            if (fileName != null)
            {
                fileName = Path.GetFullPath(fileName);

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

            InteractiveMain();
        }

        private static bool HasFlag(params string[] flags) => _args.Any(flags.Contains);

        private static void ScriptMain(TextReader input, string fileName)
        {
            var state = CreateState(false, out var _);

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
                var result = state.Run(source, fileName);

                if (result == MondValue.Undefined)
                    return;

                result.Serialize(Console.Out);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                PrintException(e);
            }
        }

        private static Func<string> _readLine;
        private static Queue<char> _input;
        private static int _line;
        private static bool _first;
        private static Highlighter _highlighter;

        private static void InteractiveMain()
        {
            var useColoredInput = !HasFlag("--no-color");

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

            var state = CreateState(true, out var options);

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

        private static void InteractiveRun(MondState state, MondProgram program)
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

        private static IEnumerable<char> ConsoleInput()
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

        private static void PrintException(Exception e)
        {
            string message = e is MondException ? e.Message : e.ToString();

            Console.WriteLine();
            Console.WriteLine(message);

            if (!message.EndsWith("\n"))
                Console.WriteLine();

            _first = true;
            _input?.Clear();
            _highlighter = null;
        }

        private static MondState CreateState(bool isInteractive, out MondCompilerOptions options)
        {
            options = new MondCompilerOptions();

            if (isInteractive)
            {
                options.MakeRootDeclarationsGlobal = true;
                options.UseImplicitGlobals = true;
            }

            var isDebug = HasFlag("--debug");
            if (isDebug)
                options.DebugInfo = MondDebugInfoLevel.Full;

            var state = new MondState
            {
                Options = options
            };

            if (isDebug)
            {
                var debugger = new MondRemoteDebugger(1597);
                state.Debugger = debugger;

                if (HasFlag("--wait"))
                {
                    debugger.RequestBreak();
                }
            }

            return state;
        }
    }
}

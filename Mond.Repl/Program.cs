using System;
using System.Collections.Generic;
using System.IO;

namespace Mond.Repl
{
    class Program
    {
        private static Queue<char> _input;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                InteractiveMain();
                return;
            }

            var state = new MondState();
            Functions.Register(state);

            string source;

            try
            {
                source = File.ReadAllText(args[0]);
            }
            catch
            {
                Console.WriteLine("Failed to read file: '{0}'", args[0]);
                return;
            }

            try
            {
                var program = MondProgram.Compile(Functions.Definitions + source, Path.GetFileName(args[0]));
                var result = state.Load(program);

                if (result != MondValue.Undefined)
                {
                    result.Serialize(Console.Out);
                    Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void InteractiveMain()
        {
            _input = new Queue<char>();

            var state = new MondState();
            var options = new MondCompilerOptions
            {
                GenerateDebugInfo = true,
                MakeRootDeclarationsGlobal = true,
                UseImplicitGlobals = true
            };

            Functions.Register(state);

            var line = 1;

            while (true)
            {
                try
                {
                    MondValue result;

                    do
                    {
                        var program = MondProgram.CompileStatement(ConsoleInput(), string.Format("stdin_{0:D}", line), options);
                        result = state.Load(program);

                        // get rid of leading whitespace
                        while (_input.Count > 0 && char.IsWhiteSpace(_input.Peek()))
                        {
                            _input.Dequeue();
                        }

                    } while (_input.Count > 0); // we only want the result of the last statement

                    line++;

                    // ignore undefined return value, it's almost always useless
                    if (result == MondValue.Undefined)
                        continue;

                    if (result["moveNext"] && result.IsEnumerable)
                    {
                        foreach (var value in result.Enumerate(state))
                        {
                            value.Serialize(Console.Out);
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        result.Serialize(Console.Out);
                        Console.WriteLine();
                    }

                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine();

                    _input.Clear();
                }
            }
        }

        static IEnumerable<char> ConsoleInput()
        {
            var first = _input.Count == 0;

            while (true)
            {
                if (_input.Count == 0)
                {
                    Console.Write(first ? "> " : ">> ");

                    var line = Console.ReadLine();
                    if (line != null)
                    {
                        if (first && line.StartsWith("="))
                            line = "return " + line.Substring(1);

                        foreach (var c in line)
                        {
                            _input.Enqueue(c);
                        }

                        _input.Enqueue('\n');
                    }

                    first = false;
                    continue;
                }

                yield return _input.Dequeue();
            }
        }
    }
}

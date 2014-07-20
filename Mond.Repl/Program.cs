using System;
using System.Collections.Generic;

namespace Mond.Repl
{
    class Program
    {
        private static Queue<char> _input; 

        static void Main()
        {
            _input = new Queue<char>();

            var state = new MondState();
            var options = new MondCompilerOptions
            {
                GenerateDebugInfo = true,
                MakeRootDeclarationsGlobal = true
            };

            while (true)
            {
                try
                {
                    MondValue result;

                    do
                    {
                        var program = MondProgram.CompileStatement(ConsoleInput(), "stdin", options);
                        result = state.Load(program);

                        // get rid of leading whitespace
                        while (_input.Count > 0 && char.IsWhiteSpace(_input.Peek()))
                        {
                            _input.Dequeue();
                        }

                    } while (_input.Count > 0); // we only want the result of the last statement

                    if (result.Type == MondValueType.Object && result.IsEnumerable)
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

                    first = false;

                    if (line != null)
                    {
                        foreach (var c in line.Trim())
                        {
                            _input.Enqueue(c);
                        }

                        _input.Enqueue('\n');
                    }

                    continue;
                }

                yield return _input.Dequeue();
            }
        }
    }
}

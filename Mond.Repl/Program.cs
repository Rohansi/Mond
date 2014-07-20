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
                    } while (_input.Count > 0);

                    Console.WriteLine(result.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        static IEnumerable<char> ConsoleInput()
        {
            var first = true;

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
                    }

                    continue;
                }

                yield return _input.Dequeue();
            }
        }
    }
}

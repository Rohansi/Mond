using System;
using Mond;

namespace MondDemo
{
    class Program
    {
        static void Main()
        {
            /*const string source1 = @"
                var a = [1, 2, 3];
                return a.length();
            ";*/

            const string source1 = @"
                fun fibonacci(n) {
                    fun inner(m, a, b) {
                        if (m == 0)
                            return a;

                        return inner(m - 1, b, a + b);
                    }

                    return inner(n, 0, 1);
                }

                return fibonacci(50);
            ";

            try
            {
                var state = new MondState();
                state["call"] = new MondFunction((_, args) => state.Call(args[0], args[1]));

                var program1 = MondProgram.Compile(source1, "test1.mnd");
                //var program2 = MondProgram.Compile(source2, "test2.mnd");

                var result1 = state.Load(program1);
                //var result2 = state.Load(program2);

                Console.WriteLine(result1.ToString());
                //Console.WriteLine(result2.ToString());
            }
            catch (MondException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }
    }
}

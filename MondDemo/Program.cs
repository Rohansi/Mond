using System;
using Mond;

namespace MondDemo
{
    class Program
    {
        static void Main()
        {
            const string source1 = @"
                seq range(start, end) {
                    for (var i = start; i <= end; i++)
                        yield i;
                }

                fun where(list, filter) -> [x : x in list, filter(x)];

                fun toArray(values) {
                    var array = [];

                    foreach (var value in values) {
                        array.add(value);
                    }

                    return array;
                }

                return range(0, 1000) |> where(fun (x) -> x % 2 == 0);
            ";

            try
            {
                var state = new MondState();
                state["call"] = new MondFunction((_, args) => state.Call(args[0], args[1]));

                state["print"] = new MondFunction((_, args) =>
                {
                    args[0].Serialize(Console.Out);
                    Console.WriteLine();
                    return MondValue.Undefined;
                });

                var program = MondProgram.Compile(source1, "test1.mnd");
                var result = state.Load(program);

                if (result.IsEnumerable)
                {
                    foreach (var i in result.Enumerate(state))
                    {
                        i.Serialize(Console.Out);
                        Console.WriteLine();
                    }
                }
                else
                {
                    result.Serialize(Console.Out);
                }
            }
            catch (MondException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }
    }
}

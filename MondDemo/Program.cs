using System;
using Mond;

namespace MondDemo
{
    class Program
    {
        static void Main()
        {
            /*const string source1 = @"
                seq fizzBuzz() {
                    var n = 1;

                    while (true) {
                        var str = '';

                        if (n % 3 == 0)
                            str += 'Fizz';
                        
                        if (n % 5 == 0)
                            str += 'Buzz';

                        if (str == '')
                            str += n;

                        yield str;
                        n++;
                    }
                };
                
                var values = [];

                foreach (var str in fizzBuzz()) {
                    values.add(str);

                    if (values.length() >= 500)
                        break;
                }

                return values;
            ";*/

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

                return range(0, 100) |> where(fun (x) -> x % 2 == 0) |> toArray();
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

                var program1 = MondProgram.Compile(source1, "test1.mnd");
                //var program2 = MondProgram.Compile(source2, "test2.mnd");

                var result1 = state.Load(program1);
                //var result2 = state.Load(program2);

                result1.Serialize(Console.Out);
                //result2.Serialize(Console.Out);
            }
            catch (MondException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }
    }
}

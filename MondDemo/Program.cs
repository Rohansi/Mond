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

            /*const string source1 = @"
                fun fibonacci(n) {
                    fun inner(m, a, b) {
                        if (m == 0)
                            return a;

                        return inner(m - 1, b, a + b);
                    }

                    return inner(n, 0, 1);
                }

                return fibonacci(50);
            ";*/

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

                foreach (var str : fizzBuzz()) {
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

                seq where(values, filter) {
                    foreach (var value : values) {
                        if (filter(value))
                            yield value;
                    }
                }

                fun toArray(values) {
                    var array = [];

                    foreach (var value : values) {
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

                var program1 = MondProgram.Compile(source1, "test1.mnd");
                //var program2 = MondProgram.Compile(source2, "test2.mnd");

                var result1 = state.Load(program1);
                //var result2 = state.Load(program2);

                foreach (var i in result1.ArrayValue)
                {
                    Console.WriteLine(i.ToString());
                }

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

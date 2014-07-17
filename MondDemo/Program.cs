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
                seq counter(start) {
                    var num = start;

                    while (true) {
                        yield start++;

                        if (start >= 10)
                            yield break;

                        yield 'beep';
                    }
                }

                var enumerator = counter(1);
                var i = 0;
                var obj = {};

                while (enumerator.moveNext()) {
                    obj[i++] = enumerator.current;

                    if (i > 25)
                        break;
                }

                return obj;
            ";*/

            const string source1 = @"
                seq fizzBuzz() {
                    var n = 0;

                    while (true) {
                        var str = '';

                        if (n % 3 == 0)
                            str += 'Fizz';
                        
                        if (n % 5 == 0)
                            str += 'Buzz';

                        if (str == '')
                            str += n;

                        n++;
                        yield str;
                    }
                }

                var enumerator = fizzBuzz();
                var i = 0;
                var obj = {};

                while (enumerator.moveNext()) {
                    obj[i++] = enumerator.current;

                    if (i > 25)
                        break;
                }

                return obj;
            
            ";

            try
            {
                var state = new MondState();
                state["call"] = new MondFunction((_, args) => state.Call(args[0], args[1]));

                var program1 = MondProgram.Compile(source1, "test1.mnd");
                //var program2 = MondProgram.Compile(source2, "test2.mnd");

                var result1 = state.Load(program1);
                //var result2 = state.Load(program2);

                foreach (var i in result1.ObjectValue)
                {
                    Console.WriteLine(i);
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

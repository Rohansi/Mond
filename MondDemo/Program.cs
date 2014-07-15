using System;
using Mond;

namespace MondDemo
{
    class Program
    {
        static void Main()
        {
            /*const string source1 = @"
                fun fib(n) {
                    if (n <= 1)
                        return n;

                    return fib(n - 1) + fib(n - 2);
                }

                return fib(35);
            ";*/

            /*const string source1 = @"
                fun fib(n) {
                    if (n == 0)
                        return 0;

                    var a = 1;
                    var b = 1;

                    for (var i = 3; i <= n; i++) {
                        var c = a + b;
                        a = b;
                        b = c;
                    }

                    return b;
                }

                return fib(50);
            ";*/

            /*const string source1 = @"
                fun startAt(x) {
                    return fun (y) -> x += y;
                }

                var counter = startAt(1);
                counter(10);
                return counter(2);
            ";*/

            /*const string source1 = @"
                fun Base() {
                    return {
                        one: fun () -> 1
                    };
                }

                fun Class() {
                    var base, inst = {
                        one: fun () -> base.one() + 1,

                        prototype: Base()
                    };

                    base = inst.prototype;
                    return inst;
                }

                var a = Class();
                return a.one();
            ";*/

            /*const string source1 = @"
                hello = fun (x) {
                    return 'hi ' + x;
                };

                return call(hello, 'nerd');
            ";

            const string source2 = @"
                return hello('brian');
            ";*/

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
                state["call"] = new MondFunction((_, inst, args) => state.Call(args[0], inst, args[1]));

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

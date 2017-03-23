using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class FunctionTests
    {
        [Test]
        public void Function()
        {
            var result = Script.Run(@"
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
            ");

            Assert.True(result == 12586269025);
        }

        [Test]
        public void DefaultReturnValue()
        {
            var result = Script.Run(@"
                fun test() {
                    return;
                }

                return test();
            ");

            Assert.True(result == MondValue.Undefined);

            result = Script.Run(@"
                fun test() {
                    
                }

                return test();
            ");

            Assert.True(result == MondValue.Undefined);
        }

        [Test]
        public void Closure()
        {
            var result = Script.Run(@"
                fun startAt(x) {
                    return fun (y) -> x += y;
                }

                var counter = startAt(1);
                counter(10);
                return counter(2);
            ");

            Assert.True(result == 13);
        }

        [Test]
        public void TailCall()
        {
            // will cause stack overflow if not tail call optimized
            var result = Script.Run(@"
                fun loop(i) {
                    if (i == 0)
                        return 'done';

                    return loop(i - 1);
                }

                return loop(10000);
            ");

            Assert.True(result == "done");

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var test = fun () {
                    return test();
                };

                return test();
            "));
        }

        [Test]
        public void VariableLengthArguments()
        {
            var result = Script.Run(@"
                fun sum(...args) {
                    var res = 0;

                    foreach (var n in args) {
                        res += n;
                    }

                    return res;
                }

                return sum(1, 2, 3);
            ");

            Assert.True(result == 6);
        }

        [Test]
        public void Unpack()
        {
            var result = Script.Run(@"
                fun add(a, b) -> a + b;
                
                fun call(function, ...args) -> function(...args);

                return call(add, 10, 5);
            ");

            Assert.True(result == 15, "single unpack");

            result = Script.Run(@"
                fun array(...values) -> values;

                return array(1, 2, 3, ...[4, 5, 6], 7, ...[8, 9, 10]);
            ");

            Assert.True(result.Array.SequenceEqual(new MondValue[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            }), "multiple unpack");
        }

        [Test]
        public void UnpackTailCall()
        {
            var result = Script.Run(@"
                fun sum(...args) {
                    switch (args.length()) {
                        case 0: return 0;
                        case 1: return args[0];
                        case 2: return args[0] + args[1];
                    }

                    return sum(args[0] + args[1], ...args[2:]);
                }

                return sum(100, 50, 10, 5, 1);
            ");

            Assert.True(result == 166);
        }

        [Test]
        public void FunctionErrors()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var a;
                fun a() { }
            "), "function name must be unique");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() { }
                test = 1;
            "), "function variable should be readonly");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun () -> 1;
            "), "don't allow unused closures");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test(a, a) { }
            "), "function arg names must be unique");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test(a, ...a) { }
            "), "function arg names must be unique");
        }

        [Test]
        public void NameRequirement()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return fun x() { return 0; };
            "));

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun () { return 0; }
            "));
        }

        [Test]
        public void LambdaExpression()
        {
            Assert.True(Script.Run("return (() -> 100)();") == 100, "simple expression");

            Assert.True(Script.Run("return (() -> { return 100; })();") == 100, "block expression");

            Assert.True(Script.Run("return (() -> { })();") == MondValue.Undefined, "empty block does nothing");

            Assert.True(Script.Run("return (() -> ({ a: 100 }))();")["a"] == 100, "return object");

            Assert.Throws<MondCompilerException>(() => Script.Run("return (() -> { a: 100 })();"), "return object no parens");
        }

        [Test]
        public void ClosureInLoop()
        {
            var result = Script.Run(@"
                var arr = [];

                for (var i = 0; i < 10; i++) {
                    var ii = i;
                    arr.add(() -> ii);
                }

                return arr[4]();
            ");

            Assert.True(result == 4);
        }

        [Test]
        public void ClosureInNestedLoop()
        {
            var result = Script.Run(@"
                var arr = [];

                for (var i = 0; i < 10; i++) {
                    var ii = i;
                    arr.add([]);

                    for (var j = 0; j < 10; j++) {
                        var jj = j;
                        arr[i].add(() -> ii / jj);
                    }
                }

                return arr[4][2]();
            ");

            Assert.True(result == 2);
        }

        [Test]
        public void UnaryUserDefinedOperator()
        {
            var state = Script.Load(@"
                seq (%%)(n) {
                    if (n == 0) {
                        yield 0;
                        return;
                    }

                    var a = 1;
                    var b = 1;

                    for (var i = 3; i <= n; i++) {
                        var c = a + b;
                        a = b;
                        b = c;

                        yield b;
                    }
                }

                global.test = %% 10;
            ");

            var expected = new[] { 2, 3, 5, 8, 13, 21, 34, 55 }.Select(n => new MondValue(n));
            Assert.True(state["test"].Enumerate(state).SequenceEqual(expected));
        }

        [Test]
        public void BinaryUserDefinedOperator()
        {
            var result = Script.Run(@"
                fun (>>>)(fun1, fun2) {
                    return fun(... args) {
                        return fun1(... args) |> fun2();
                    };
                }

                fun double(n) -> n *  2;
                fun square(n) -> n ** 2;

                return (square >>> double)(5);
            ");

            Assert.True(result == 50);
        }

        [Test]
        public void FunctionDecorators()
        {
            // test the execution order
            var state = Script.Load(@"
                global.result = [];

                fun add(func, num) {
                    global.result.add(num);

                    return (... args) -> func(... args);
                }

                @add(1)
                @add(2)
                fun test() {}

                test();
            ");

            var expected = new[] { 1, 2 }.Select(n => new MondValue(n));
            Assert.True(state["result"].Enumerate(state).SequenceEqual(expected));

            // test nesting
            var result = Script.Run(@"
                fun mult(func, x) -> fun(... args) -> func(... args) * x;

                fun add( x, y ) {
                    @mult( 2 )
                    fun test( z ) -> z;

                    return test( x + y );
                }

                return add( 5, 10 );
            ");

            Assert.AreEqual((int)result, 30);
        }
    }
}

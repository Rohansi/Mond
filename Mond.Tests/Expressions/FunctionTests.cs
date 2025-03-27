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

            Assert.AreEqual((MondValue)12586269025, result);
        }

        [Test]
        public void FunctionWriteToArgument()
        {
            var result = Script.Run(@"
                fun foo(n) {
                    n += n;
                    return n;
                }

                return foo(50);
            ");

            Assert.AreEqual((MondValue)100, result);
        }

        [Test]
        public void FunctionWriteToUnspecifiedArgument()
        {
            var result = Script.Run(@"
                fun foo(n) {
                    n = n || 'default value';
                    return n;
                }

                return foo();
            ");

            Assert.AreEqual((MondValue)"default value", result);
        }

        [Test]
        public void DefaultReturnValue()
        {
            var result = Script.Run(@"
                fun test() {
                    
                }

                return test();
            ");

            Assert.AreEqual(MondValue.Undefined, result);
        }

        [Test]
        public void DefaultReturnValueExplicit()
        {
            var result = Script.Run(@"
                fun test() {
                    return;
                }

                return test();
            ");

            Assert.AreEqual(MondValue.Undefined, result);
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

            Assert.AreEqual((MondValue)13, result);
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

            Assert.AreEqual((MondValue)"done", result);
        }

        [Test]
        public void NoTailCallStackOverflow()
        {
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

            Assert.AreEqual((MondValue)6, result);
        }

        [Test]
        public void Unpack()
        {
            var result = Script.Run(@"
                fun add(a, b) -> a + b;
                
                fun call(function, ...args) -> function(...args);

                return call(add, 10, 5);
            ");

            Assert.AreEqual((MondValue)15, result);
        }

        [Test]
        public void UnpackMultiple()
        {
            var result = Script.Run(out var state, @"
                fun array(...values) -> values;

                return array(1, 2, 3, ...[4, 5, 6], 7, ...[8, 9, 10]);
            ");

            var expected = new MondValue[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };

            CollectionAssert.AreEqual(expected, result.Enumerate(state));
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

            Assert.AreEqual((MondValue)166, result);
        }

        [Test]
        public void FunctionNameUniqueness()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var a;
                fun a() { }
            "));
        }

        [Test]
        public void FunctionNameReadonly()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() { }
                test = 1;
            "));
        }

        [Test]
        public void UnusedClosure()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun () -> 1;
            "));
        }

        [Test]
        public void FunctionParameterUniqueness()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test(a, a) { }
            "));
        }

        [Test]
        public void FunctionParameterUniquenessPack()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test(a, ...a) { }
            "), "function arg names must be unique");
        }

        [Test]
        public void FunctionExpressionWithName()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return fun x() { return 0; };
            "));
        }

        [Test]
        public void FunctionStatementWithNoName()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun () { return 0; }
            "));
        }

        [Test]
        public void FunctionExpressionVariable()
        {
            const string code =
                """
                var increment = fun(x) {
                    return x + 1;
                };
                return increment(2);
                """;

            Assert.DoesNotThrow(() => MondProgram.Compile(code));

            var result = Script.Run(code);
            Assert.AreEqual((MondValue)3, result);
        }

        [Test]
        public void LambdaExpressionSimple()
        {
            Assert.AreEqual((MondValue)100, Script.Run("return (() -> 100)();"));
        }

        [Test]
        public void LambdaExpressionBlock()
        {
            Assert.AreEqual((MondValue)100, Script.Run("return (() -> { return 100; })();"));
        }

        [Test]
        public void LambdaExpressionEmptyBlock()
        {
            Assert.AreEqual(MondValue.Undefined, Script.Run("return (() -> { })();"));
        }

        [Test]
        public void LambdaExpressionReturnObject()
        {
            Assert.AreEqual((MondValue)100, Script.Run("return (() -> ({ a: 100 }))();")["a"]);
        }

        [Test]
        public void LambdaExpressionReturnObjectWithoutBrackets()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run("return (() -> { a: 100 })();"));
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

            Assert.AreEqual((MondValue)4, result);
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

            Assert.AreEqual((MondValue)2, result);
        }

        [Test]
        public void UnaryUserDefinedOperator()
        {
            var result = Script.Run(out var state, @"
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

                return %% 10;
            ");

            var expected = new MondValue[]
            {
                2, 3, 5, 8, 13, 21, 34, 55
            };

            CollectionAssert.AreEqual(expected, result.Enumerate(state));
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

            Assert.AreEqual((MondValue)50, result);
        }

        [Test]
        public void DecoratorExecutionOrder()
        {
            var result = Script.Run(out var state, @"
                var result = [];

                fun add(func, num) {
                    result.add(num);

                    return (... args) -> func(... args);
                }

                @add(1)
                @add(2)
                fun test() {}

                test();

                return result;
            ");

            var expected = new MondValue[]
            {
                1, 2
            };

            CollectionAssert.AreEqual(expected, result.Enumerate(state));
        }

        [Test]
        public void FunctionDecorators()
        {
            var result = Script.Run(@"
                fun mult(func, x) -> fun(... args) -> func(... args) * x;

                fun add( x, y ) {
                    @mult( 2 )
                    fun test( z ) -> z;

                    return test( x + y );
                }

                return add( 5, 10 );
            ");
            
            Assert.AreEqual((MondValue)30, result);
        }

        [Test]
        public void FunctionGetName()
        {
            var result = Script.Run(@"
                fun Outer() {
                    fun inner() { }
                    return inner;
                }

                return Outer().getName();
            ");

            Assert.AreEqual(MondValueType.String, result.Type);
            Assert.True(result == "Outer.inner");
        }

        [Test]
        public void BacktickInfixFunction()
        {
            // test basic functionality
            var result = Script.Run(@"
                fun like(a, b) -> a.toLower() == b.toLower();

                return 'FOO' `like` 'foo';
            ");

            Assert.True(result);

            // test chaining
            result = Script.Run(@"
                seq to(begin, end) {
                    for (var i = begin; i <= end; ++i)
                        yield i;
                }

                fun fold(enumerable, fn) {
                    const enumerator = enumerable.getEnumerator();
                    enumerator.moveNext();

                    var accumulator = enumerator.current;
                    while (enumerator.moveNext())
                        accumulator = fn( accumulator, enumerator.current );

                    enumerator.dispose();
                    return accumulator;
                }

                return 1 `to` 5 `fold` (+);
            ");

            Assert.AreEqual((MondValue)15, result);
        }
        
        [Test]
        public void UndefinedFunctionErrorMessage()
        {
            var ex = Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var obj = {};
                return obj.testMethod();
            "));

            StringAssert.Contains("testMethod", ex?.Message);
        }
        
        [Test]
        public void UndefinedFunctionErrorMessageWithUnpacks()
        {
            var ex = Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var obj = {};
                return obj.testMethod(...[1]);
            "));

            StringAssert.Contains("testMethod", ex?.Message);
        }

        [Test]
        public void FunctionExecutionOrder()
        {
            var script =
                """
                const arr = [];
                fun x() { arr.add(1); return () -> {}; }
                fun y() { arr.add(2); }
                fun z() { arr.add(3); }
                x()(y(), z());
                return arr;
                """;
            var result = Script.Run(script);
            CollectionAssert.AreEqual(result.ArrayValue, new MondValue[] { 1, 2, 3 });
        }

        [Test]
        public void FunctionInstanceCall()
        {
            const string script =
                """
                const prototype = {
                    method: fun (this, x, y) -> this.value + x + y,
                };
                const obj = { value: 10 };
                obj.setPrototype(prototype);
                return obj.method(1, 2);
                """;

            var result = Script.Run(script);
            Assert.AreEqual((MondValue)13, result);
        }

        [Test]
        public void FunctionNoInstanceCallOnGlobal()
        {
            const string script =
                """
                global.method = fun (x, y) -> x + y;
                return global.method(1, 2);
                """;

            var result = Script.Run(script);
            Assert.AreEqual((MondValue)3, result);
        }

        [Test]
        public void FunctionNoInstanceCallOnCapitalized()
        {
            const string script =
                """
                const Module = {
                    method: fun (x, y) -> x + y,
                };
                return Module.method(1, 2);
                """;

            var result = Script.Run(script);
            Assert.AreEqual((MondValue)3, result);
        }
    }
}

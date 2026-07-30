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

            Assert.That(result, Is.EqualTo((MondValue)12586269025));
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

            Assert.That(result, Is.EqualTo((MondValue)100));
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

            Assert.That(result, Is.EqualTo((MondValue)"default value"));
        }

        [Test]
        public void DefaultReturnValue()
        {
            var result = Script.Run(@"
                fun test() {
                    
                }

                return test();
            ");

            Assert.That(result, Is.EqualTo(MondValue.Undefined));
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

            Assert.That(result, Is.EqualTo(MondValue.Undefined));
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

            Assert.That(result, Is.EqualTo((MondValue)13));
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

            Assert.That(result, Is.EqualTo((MondValue)"done"));
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

            Assert.That(result, Is.EqualTo((MondValue)6));
        }

        [Test]
        public void Unpack()
        {
            var result = Script.Run(@"
                fun add(a, b) -> a + b;
                
                fun call(function, ...args) -> function(...args);

                return call(add, 10, 5);
            ");

            Assert.That(result, Is.EqualTo((MondValue)15));
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

            Assert.That(result.Enumerate(state), Is.EqualTo(expected));
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

            Assert.That(result, Is.EqualTo((MondValue)166));
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
            Assert.That(result, Is.EqualTo((MondValue)3));
        }

        [Test]
        public void LambdaExpressionSimple()
        {
            Assert.That(Script.Run("return (() -> 100)();"), Is.EqualTo((MondValue)100));
        }

        [Test]
        public void LambdaExpressionBlock()
        {
            Assert.That(Script.Run("return (() -> { return 100; })();"), Is.EqualTo((MondValue)100));
        }

        [Test]
        public void LambdaExpressionEmptyBlock()
        {
            Assert.That(Script.Run("return (() -> { })();"), Is.EqualTo(MondValue.Undefined));
        }

        [Test]
        public void LambdaExpressionReturnObject()
        {
            Assert.That(Script.Run("return (() -> ({ a: 100 }))();")["a"], Is.EqualTo((MondValue)100));
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

            Assert.That(result, Is.EqualTo((MondValue)4));
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

            Assert.That(result, Is.EqualTo((MondValue)2));
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

            Assert.That(result.Enumerate(state), Is.EqualTo(expected));
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

            Assert.That(result, Is.EqualTo((MondValue)50));
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

            Assert.That(result.Enumerate(state), Is.EqualTo(expected));
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
            
            Assert.That(result, Is.EqualTo((MondValue)30));
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

            Assert.That(result.Type, Is.EqualTo(MondValueType.String));
            Assert.That(result == "Outer.inner", Is.True);
        }

        [Test]
        public void BacktickInfixFunction()
        {
            // test basic functionality
            var result = Script.Run(@"
                fun like(a, b) -> a.toLower() == b.toLower();

                return 'FOO' `like` 'foo';
            ");

            Assert.That((bool)result, Is.True);

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

            Assert.That(result, Is.EqualTo((MondValue)15));
        }
        
        [Test]
        public void UndefinedFunctionErrorMessage()
        {
            var ex = Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var obj = {};
                return obj.testMethod();
            "));

            Assert.That(ex?.Message, Does.Contain("testMethod"));
        }
        
        [Test]
        public void UndefinedFunctionErrorMessageWithUnpacks()
        {
            var ex = Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var obj = {};
                return obj.testMethod(...[1]);
            "));

            Assert.That(ex?.Message, Does.Contain("testMethod"));
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
            Assert.That(new MondValue[] { 1, 2, 3 }, Is.EqualTo(result.ArrayValue));
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
            Assert.That(result, Is.EqualTo((MondValue)13));
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
            Assert.That(result, Is.EqualTo((MondValue)3));
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
            Assert.That(result, Is.EqualTo((MondValue)3));
        }
    }
}

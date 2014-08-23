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
        public void Classes()
        {
            var result = Script.Run(@"
                fun Base() {
                    return {
                        number: fun () -> 10,
                        add: fun (x, y) -> x + y
                    };
                }

                fun Class() {
                    var base, this = {
                        number: fun () -> this.add(base.number(), 5)
                    };

                    base = Base();
                    this.prototype(base);

                    return this;
                }

                var a = Class();
                return a.number();
            ");

            Assert.True(result == 15);
        }

        [Test]
        public void TailCall()
        {
            var result = Script.Run(@"
                fun loop(i) {
                    if (i == 0)
                        return 'done';

                    return loop(i - 1);
                }

                return loop(10);
            ");

            Assert.True(result == "done");
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
        public void DoubleAssign()
        {
            var result = Script.Run(@"
                var a = fun b() -> 1;
                return a() + b();
            ");

            Assert.True(result == 2);
        }
    }
}

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
                        number: fun () -> 10
                    };
                }

                fun Class() {
                    var base, this = {
                        number: fun () -> base.number() + 5,

                        prototype: Base()
                    };

                    base = this.prototype;
                    return this;
                }

                var a = Class();
                return a.number();
            ");

            Assert.True(result == 15);
        }
    }
}

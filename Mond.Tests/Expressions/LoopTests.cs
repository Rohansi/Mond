using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class LoopTests
    {
        [Test]
        public void While()
        {
            var result = Script.Run(@"
                var a = 0;

                while (a < 500) {
                    if (a < 2) {
                        a++;
                        continue;
                    }

                    if (a > 100)
                        break;

                    a *= a;
                }

                return a;
            ");

            Assert.True(result == 256);
        }

        [Test]
        public void DoWhile()
        {
            var result = Script.Run(@"
                var a = 0;

                do {
                    if (a < 2) {
                        a++;
                        continue;
                    }

                    if (a > 2000)
                        break;

                    a *= 2;
                } while (a < 1000);

                return a;
            ");

            Assert.True(result == 1024);
        }

        [Test]
        public void For()
        {
            var result = Script.Run(@"
                var a = 1;

                for (var i = 2; i <= 10; i++) {
                    a *= i;
                }

                return a;
            ");

            Assert.True(result == 3628800);
        }

        [Test]
        public void ForInfinite()
        {
            var result = Script.Run(@"
                var i = 0;

                for (;;) {
                    i++;

                    if (i >= 100)
                        break;
                }

                return i;
            ");

            Assert.True(result == 100);
        }

        [Test]
        public void ForNested()
        {
            var result = Script.Run(@"
                var result = 0;

                for (var i = 0; i < 10; i++) {
                    if (i % 2 == 0)
                        continue;

                    for (var j = 0; j < 100; j++) {
                        result++;
                    }
                }

                return result;
            ");

            Assert.True(result == 500);
        }
    }
}

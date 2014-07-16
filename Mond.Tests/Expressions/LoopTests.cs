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
            var result1 = Script.Run(@"
                var a = 1;

                for (var i = 2; i <= 10; i++) {
                    a *= i;
                }

                return a;
            ");

            Assert.True(result1 == 3628800);

            var result2 = Script.Run(@"
                var i = 0;

                for (;;) {
                    i++;

                    if (i >= 100)
                        break;
                }

                return i;
            ");

            Assert.True(result2 == 100);
        }
    }
}

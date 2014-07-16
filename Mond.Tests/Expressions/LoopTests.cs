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
            // TODO
        }

        [Test]
        public void For()
        {
            // TODO
        }
    }
}

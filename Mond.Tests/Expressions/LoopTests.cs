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

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var i;
                for (var i = 0; i < 10; i++) { }
            "), "for loop identifier must be unique");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                for (break;;) {} 
            "), "for loop initializer must be var");
        }

        [Test]
        public void ForMultipleIncrement()
        {
            var result = Script.Run(@"
                var res = 0;

                for (var i = 1; i <= 10; i++, i++) {
                    res++;
                }

                return res;
            ");

            Assert.True(result == 5);
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

        [Test]
        public void Foreach()
        {
            var result = Script.Run(@"
                var values = [1, 2, 3, 4, 5];
                var res = 0;

                foreach (var n in values) {
                    res += n;
                }

                return res;
            ");

            Assert.True(result == 15);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var i;
                foreach (var i in []) { }
            "));
        }

        [Test]
        public void Break()
        {
            var result = Script.Run(@"
                var res = 0;

                for (var i = 1; i <= 10; i++) {
                    if (i > 5)
                        break;

                    res += i;
                }

                return res;
            ");

            Assert.True(result == 15);

            Assert.Throws<MondCompilerException>(() => Script.Run("break;"));
        }

        [Test]
        public void Continue()
        {
            var result = Script.Run(@"
                var res = 0;

                for (var i = 1; i <= 10; i++) {
                    if (i % 2 != 0)
                        continue;

                    res += i;
                }

                return res;
            ");

            Assert.True(result == 30);

            Assert.Throws<MondCompilerException>(() => Script.Run("continue;"));
        }
    }
}

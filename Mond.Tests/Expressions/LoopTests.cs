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

            result = Script.Run(@"
                var i = 0;
                var arr = [];
            
                while (i < 10) {
                    var ii = i;
                    arr.add(() -> ii);

                    i++;

                    if (i >= 5) break;
                    if (i == 2) continue;
                }

                return arr[4]();
            ");

            Assert.True(result == 4, "closure in loop");
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

            result = Script.Run(@"
                var i = 0;
                
                do {
                    continue;
                } while (++i < 10);
                
                return i;
            ");

            Assert.True(result == 10);

            result = Script.Run(@"
                var i = 0;
                var arr = [];

                do {
                    var ii = i;
                    arr.add(() -> ii);

                    if (i >= 5) break;
                    if (i == 2) continue;
                } while (++i < 10);

                return arr[4]();
            ");

            Assert.True(result == 4, "closure in loop");
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

            result = Script.Run(@"
                var arr = [];
            
                for (var i = 0; i < 10; i++) {
                    var ii = i;
                    arr.add(() -> ii);

                    if (i >= 5) break;
                    if (i == 2) continue;
                }

                return arr[4]();
            ");

            Assert.True(result == 4, "closure in loop");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var i;
                for (var i = 0; i < 10; i++) { }
            "), "for loop identifier must be unique");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                for (break;;) {} 
            "), "for loop initializer must be var");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                for (;; return) {}
            "), "for loop increment must be expression");
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

            result = Script.Run(@"
                var arr = [];

                foreach (var i in [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]) {
                    var ii = i;
                    arr.add(() -> ii);

                    if (i >= 5) break;
                    if (i == 2) continue;
                }

                return arr[4]();
            ");

            Assert.True(result == 4, "closure in loop");

            result = Script.Run(@"
                foreach (var i in []) { }
                foreach (var i in []) { }
                return 1;
            ");

            Assert.True(result == 1, "two foreach in same scope with same ident");

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

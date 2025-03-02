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

            Assert.AreEqual((MondValue)256, result);
        }

        [Test]
        public void WhileClosure()
        {
            var result = Script.Run(@"
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

            Assert.AreEqual((MondValue)4, result);
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

            Assert.AreEqual((MondValue)1024, result);
        }

        [Test]
        public void DoWhileContinue()
        {
            var result = Script.Run(@"
                var i = 0;
                
                do {
                    continue;
                } while (++i < 10);
                
                return i;
            ");

            Assert.AreEqual((MondValue)10, result);
        }

        [Test]
        public void DoWhileClosure()
        {
            var result = Script.Run(@"
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

            Assert.AreEqual((MondValue)4, result);
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

            Assert.AreEqual((MondValue)3628800, result);
        }

        [Test]
        public void ForClosure()
        {
            var result = Script.Run(@"
                var arr = [];
            
                for (var i = 0; i < 10; i++) {
                    var ii = i;
                    arr.add(() -> ii);

                    if (i >= 5) break;
                    if (i == 2) continue;
                }

                return arr[4]();
            ");

            Assert.AreEqual((MondValue)4, result);
        }

        [Test]
        public void ForReuseIdentifier()
        {
            var result = Script.Run(@"
                for (var i = 0; i < 10; i++) { }
                for (var i = 0; i < 10; i++) { }
                return 1;
            ");

            Assert.AreEqual((MondValue)1, result);
        }

        [Test]
        public void ForIdentifierUniqueness()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var i;
                for (var i = 0; i < 10; i++) { }
            "));
        }

        [Test]
        public void ForInitializer()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                for (break;;) {} 
            "));
        }

        [Test]
        public void ForIncrement()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                for (;; return) {}
            "));
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

            Assert.AreEqual((MondValue)5, result);
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

            Assert.AreEqual((MondValue)100, result);
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

            Assert.AreEqual((MondValue)500, result);
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

            Assert.AreEqual((MondValue)15, result);
        }

        [Test]
        public void ForeachClosure()
        {
            var result = Script.Run(@"
                var arr = [];

                foreach (var i in [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]) {
                    var ii = i;
                    arr.add(() -> ii);

                    if (i >= 5) break;
                    if (i == 2) continue;
                }

                return arr[4]();
            ");

            Assert.AreEqual((MondValue)4, result);
        }

        [Test]
        public void ForeachReuseIdentifier()
        {
            var result = Script.Run(@"
                foreach (var i in []) { }
                foreach (var i in []) { }
                return 1;
            ");

            Assert.AreEqual((MondValue)1, result);
        }

        [Test]
        public void ForeachIdentifierUniqueness()
        {
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

            Assert.AreEqual((MondValue)15, result);
        }

        [Test]
        public void BreakNotInLoop()
        {
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

            Assert.AreEqual((MondValue)30, result);
        }

        [Test]
        public void ContinueNotInLoop()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run("continue;"));
        }
    }
}

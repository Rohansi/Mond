using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class DecoratorTests
    {
        [Test]
        public void FunctionDecorator()
        {
            var result = Script.Run(@"
                fun double(x) -> (...args) -> x(...args) * 2;

                @double
                fun add(x, y) -> x + y;

                return add(2, 2);
            ");

            Assert.AreEqual((MondValue)8, result);
        }

        [Test]
        public void FunctionDecorator_Seq()
        {
            var result = Script.Run(@"
                fun double(x) {
                    return seq (...args) {
                        foreach (var e in x(...args)) {
                            yield e * 2;
                        }
                    };
                }

                @double
                seq count() {
                    for (var i = 1; i < 3; i++) {
                        yield i;
                    }
                }

                foreach (var e in count()) {
                    return e;
                }
            ");

            Assert.AreEqual((MondValue)2, result);
        }

        [Test]
        public void FunctionDecorator_Readonly()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun double(x) -> (...args) -> x(...args) * 2;

                @double
                fun add(x, y) -> x + y;

                add = 10;
            "));
        }

        [Test]
        public void VariableDecorator()
        {
            var result = Script.Run(@"
                fun double(x) -> x + x;

                @double
                const value = 2;

                return value;
            ");

            Assert.AreEqual((MondValue)4, result);
        }

        [Test]
        public void VariableDecorator_PreserveReadonly()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun double(x) -> x + x;

                @double
                const value = 2;

                value = 10;
            "));

            var result = Script.Run(@"
                fun double(x) -> x + x;

                @double
                var value = 2;

                value = 10;
                return value;
            ");

            Assert.AreEqual((MondValue)10, result);
        }
    }
}

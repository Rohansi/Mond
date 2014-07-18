using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ValueTests
    {
        private MondValue _result;

        [Test]
        public void Add()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a + b;
            ");

            Assert.True(_result == 110);

            _result = Script.Run(@"
                var a = 'test', b = 10;
                return a + b;
            ");

            Assert.True(_result == "test10");

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = null, b = 10;
                return a + b;
            "));
        }

        [Test]
        public void Subtract()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a - b;
            ");

            Assert.True(_result == 90);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 10;
                return a - b;
            "));
        }

        [Test]
        public void Multiply()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a * b;
            ");

            Assert.True(_result == 1000);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 10;
                return a * b;
            "));
        }

        [Test]
        public void Divide()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a / b;
            ");

            Assert.True(_result == 10);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 10;
                return a / b;
            "));
        }

        [Test]
        public void Modulo()
        {
            _result = Script.Run(@"
                var a = 101, b = 10;
                return a % b;
            ");

            Assert.True(_result == 1);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 10;
                return a % b;
            "));
        }
    }
}

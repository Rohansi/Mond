using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ValueTests
    {
        private MondValue _result;

        [Test]
        public void NumberParse()
        {
            Assert.True(Script.Run("return 100;") == 100, "simple");

            Assert.True(Script.Run("return 1_000;") == 1000, "simple with separator");

            Assert.True(Script.Run("return 100.35;") == 100.35, "decimal");

            Assert.True(Script.Run("return -100;") == -100, "sign");

            Assert.True(Script.Run("return 10e4;") == 10e4, "exponent");

            Assert.True(Script.Run("return 10e40;") == 10e40, "big exponent");

            Assert.True(Script.Run("return 10e+4;") == 10e+4, "exponent sign +");

            Assert.True(Script.Run("return 10e-4;") == 10e-4, "exponent sign -");

            Assert.True(Script.Run("return 0x1234ABCD;") == 0x1234ABCD, "hex number");

            Assert.True(Script.Run("return 0x1234_ABCD;") == 0x1234ABCD, "hex number with separator");

            Assert.True(Script.Run("return 0b00010010001101001010101111001101;") == 0x1234ABCD, "binary number");

            Assert.True(Script.Run("return 0b00010010_00110100_10101011_11001101;") == 0x1234ABCD, "binary number with separator");

            Assert.True(Script.Run("return 0xDEADBEEF;") == unchecked((int)0xDEADBEEF), "negative hex number");

            Assert.True(Script.Run("return 0b11011110101011011011111011101111;") == unchecked((int)0xDEADBEEF), "negative binary number");
        }

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

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 += 2;
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

        [Test]
        public void Exponent()
        {
            _result = Script.Run(@"
                var a = 2, b = 8;
                return a ** b;
            ");

            Assert.True(_result == 256);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 10;
                return a ** b;
            "));
        }

        [Test]
        public void LeftShift()
        {
            _result = Script.Run(@"
                var a = 2, b = 4;
                return a << b;
            ");

            Assert.True(_result == 32);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 4;
                return a << b;
            "));
        }

        [Test]
        public void RightShift()
        {
            _result = Script.Run(@"
                var a = 64, b = 2;
                return a >> b;
            ");

            Assert.True(_result == 16);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 2;
                return a << b;
            "));
        }

        [Test]
        public void BitOr()
        {
            _result = Script.Run(@"
                var a = 2, b = 4;
                return a | b;
            ");

            Assert.True(_result == 6);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 4;
                return a | b;
            "));
        }

        [Test]
        public void BitAnd()
        {
            _result = Script.Run(@"
                var a = 2 | 4, b = 4;
                return a & b;
            ");

            Assert.True(_result == 4);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 4;
                return a & b;
            "));
        }

        [Test]
        public void BitXor()
        {
            _result = Script.Run(@"
                var a = 2 | 4, b = 4;
                return a ^ b;
            ");

            Assert.True(_result == 2);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 4;
                return a ^ b;
            "));
        }

        [Test]
        public void Increment()
        {
            _result = Script.Run(@"
                var a = 0;
                return a++;
            ");

            Assert.True(_result == 0);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1++;
            "));

            _result = Script.Run(@"
                var a = 0;
                return ++a;
            ");

            Assert.True(_result == 1);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return ++1;
            "));
        }

        [Test]
        public void Decrement()
        {
            _result = Script.Run(@"
                var a = 0;
                return a--;
            ");

            Assert.True(_result == 0);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1--;
            "));

            _result = Script.Run(@"
                var a = 0;
                return --a;
            ");

            Assert.True(_result == -1);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return --1;
            "));
        }

        [Test]
        public void Constants()
        {
            Assert.True(Script.Run("return null;") == MondValue.Null);

            Assert.True(Script.Run("return undefined;") == MondValue.Undefined);

            Assert.True(double.IsNaN(Script.Run("return NaN;")));

            Assert.True(double.IsInfinity(Script.Run("return Infinity;")));

            Assert.True(double.IsNegativeInfinity(Script.Run("return -Infinity;")));

            Assert.True(Script.Run("return true;"));

            Assert.False(Script.Run("return false;"));
        }

        [Test]
        public void Negate()
        {
            _result = Script.Run(@"
                var a = 100;
                return -a;
            ");

            Assert.True(_result == -100);
        }

        [Test]
        public void BitNot()
        {
            _result = Script.Run(@"
                var a = 100;
                return ~a;
            ");

            Assert.True(_result == -101);
        }
    }
}

using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ValueTests
    {
        private MondValue _result;

        [Test]
        [TestCase("100", 100)]
        [TestCase("1_000", 1000)]
        [TestCase("100.35", 100.35)]
        [TestCase("-100", -100)]
        [TestCase("10e4", 10e4)]
        [TestCase("10e40", 10e40)]
        [TestCase("10e+4", 10e+4)]
        [TestCase("10e-4", 10e-4)]
        [TestCase("0x1234ABCD", 0x1234ABCD)]
        [TestCase("0x1234_ABCD", 0x1234ABCD)]
        [TestCase("0b00010010001101001010101111001101", 0x1234ABCD)]
        [TestCase("0b00010010_00110100_10101011_11001101", 0x1234ABCD)]
        [TestCase("0xDEADBEEF", unchecked((int)0xDEADBEEF))]
        [TestCase("0b11011110101011011011111011101111", unchecked((int)0xDEADBEEF))]
        public void NumberParse(string expression, double expected) =>
            Assert.That(Script.Run($"return {expression};"), Is.EqualTo((MondValue)expected));

        [Test]
        public void Add()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a + b;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)110));
        }

        [Test]
        public void AddImplicitToStringRight()
        {
            _result = Script.Run(@"
                var a = 'test', b = 10;
                return a + b;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)"test10"));
        }

        [Test]
        public void AddImplicitToStringLeft()
        {
            _result = Script.Run(@"
                var a = 10, b = 'test';
                return a + b;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)"10test"));
        }

        [Test]
        public void AddInvalidType()
        {
            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = null, b = 10;
                return a + b;
            "));
        }

        [Test]
        public void AddAssignmentNotStorable()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)90));
        }

        [Test]
        public void SubtractInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)1000));
        }

        [Test]
        public void MultiplyInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void DivideInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)1));
        }

        [Test]
        public void ModuloInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)256));
        }

        [Test]
        public void ExponentInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)32));
        }

        [Test]
        public void LeftShiftInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)16));
        }

        [Test]
        public void RightShiftInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)6));
        }

        [Test]
        public void BitOrInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)4));
        }

        [Test]
        public void BitAndInvalidType()
        {
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

            Assert.That(_result, Is.EqualTo((MondValue)2));
        }

        [Test]
        public void BitXorInvalidType()
        {
            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var a = 'test', b = 4;
                return a ^ b;
            "));
        }

        [Test]
        public void IncrementSuffix()
        {
            _result = Script.Run(@"
                var a = 0;
                a++;
                return a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)1));
        }

        [Test]
        public void IncrementSuffixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ a++, a ];
            ");

            var expected = new MondValue[] { 0, 1 };
            Assert.That(_result.AsList, Is.EqualTo(expected));
        }

        [Test]
        public void IncrementSuffixNotStorable()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1++;
            "));
        }

        [Test]
        public void IncrementPrefix()
        {
            _result = Script.Run(@"
                var a = 0;
                ++a;
                return a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)1));
        }

        [Test]
        public void IncrementPrefixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ ++a, a ];
            ");

            var expected = new MondValue[] { 1, 1 };
            Assert.That(_result.AsList, Is.EqualTo(expected));
        }

        [Test]
        public void IncrementPrefixNotStorable()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return ++1;
            "));
        }

        [Test]
        public void DecrementSuffix()
        {
            _result = Script.Run(@"
                var a = 0;
                a--;
                return a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)(-1)));
        }

        [Test]
        public void DecrementSuffixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ a--, a ];
            ");

            var expected = new MondValue[] { 0, -1 };
            Assert.That(_result.AsList, Is.EqualTo(expected));
        }

        [Test]
        public void DecrementSuffixNotStorable()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1--;
            "));
        }

        [Test]
        public void DecrementPrefix()
        {
            _result = Script.Run(@"
                var a = 0;
                --a;
                return a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)(-1)));
        }

        [Test]
        public void DecrementPrefixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ --a, a ];
            ");


            var expected = new MondValue[] { -1, -1 };
            Assert.That(_result.AsList, Is.EqualTo(expected));
        }

        [Test]
        public void DecrementPrefixNotStorable()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return --1;
            "));
        }

        [Test]
        public void Constants()
        {
            Assert.That(Script.Run("return null;"), Is.EqualTo(MondValue.Null));

            Assert.That(Script.Run("return undefined;"), Is.EqualTo(MondValue.Undefined));

            Assert.That(Script.Run("return true;"), Is.EqualTo(MondValue.True));

            Assert.That(Script.Run("return false;"), Is.EqualTo(MondValue.False));

            Assert.That(double.IsNaN(Script.Run("return NaN;")), Is.True);

            Assert.That(double.IsInfinity(Script.Run("return Infinity;")), Is.True);

            Assert.That(double.IsNegativeInfinity(Script.Run("return -Infinity;")), Is.True);
        }

        [Test]
        public void Negate()
        {
            _result = Script.Run(@"
                var a = 100;
                return -a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)(-100)));
        }

        [Test]
        public void BitNot()
        {
            _result = Script.Run(@"
                var a = 100;
                return ~a;
            ");

            Assert.That(_result, Is.EqualTo((MondValue)(-101)));
        }
    }
}

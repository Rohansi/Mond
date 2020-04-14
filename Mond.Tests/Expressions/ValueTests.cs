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
            Assert.AreEqual((MondValue)expected, Script.Run($"return {expression};"));

        [Test]
        public void Add()
        {
            _result = Script.Run(@"
                var a = 100, b = 10;
                return a + b;
            ");

            Assert.AreEqual((MondValue)110, _result);
        }

        [Test]
        public void AddImplicitToStringRight()
        {
            _result = Script.Run(@"
                var a = 'test', b = 10;
                return a + b;
            ");

            Assert.AreEqual((MondValue)"test10", _result);
        }

        [Test]
        public void AddImplicitToStringLeft()
        {
            _result = Script.Run(@"
                var a = 10, b = 'test';
                return a + b;
            ");

            Assert.AreEqual((MondValue)"10test", _result);
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

            Assert.AreEqual((MondValue)90, _result);
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

            Assert.AreEqual((MondValue)1000, _result);
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

            Assert.AreEqual((MondValue)10, _result);
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

            Assert.AreEqual((MondValue)1, _result);
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

            Assert.AreEqual((MondValue)256, _result);
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

            Assert.AreEqual((MondValue)32, _result);
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

            Assert.AreEqual((MondValue)16, _result);
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

            Assert.AreEqual((MondValue)6, _result);
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

            Assert.AreEqual((MondValue)4, _result);
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

            Assert.AreEqual((MondValue)2, _result);
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

            Assert.AreEqual((MondValue)1, _result);
        }

        [Test]
        public void IncrementSuffixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ a++, a ];
            ");

            var expected = new MondValue[] { 0, 1 };
            CollectionAssert.AreEqual(expected, _result.AsList);
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

            Assert.AreEqual((MondValue)1, _result);
        }

        [Test]
        public void IncrementPrefixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ ++a, a ];
            ");

            var expected = new MondValue[] { 1, 1 };
            CollectionAssert.AreEqual(expected, _result.AsList);
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

            Assert.AreEqual((MondValue)(-1), _result);
        }

        [Test]
        public void DecrementSuffixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ a--, a ];
            ");

            var expected = new MondValue[] { 0, -1 };
            CollectionAssert.AreEqual(expected, _result.AsList);
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

            Assert.AreEqual((MondValue)(-1), _result);
        }

        [Test]
        public void DecrementPrefixResult()
        {
            _result = Script.Run(@"
                var a = 0;
                return [ --a, a ];
            ");


            var expected = new MondValue[] { -1, -1 };
            CollectionAssert.AreEqual(expected, _result.AsList);
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
            Assert.AreEqual(MondValue.Null, Script.Run("return null;"));

            Assert.AreEqual(MondValue.Undefined, Script.Run("return undefined;"));

            Assert.AreEqual(MondValue.True, Script.Run("return true;"));

            Assert.AreEqual(MondValue.False, Script.Run("return false;"));

            Assert.True(double.IsNaN(Script.Run("return NaN;")));

            Assert.True(double.IsInfinity(Script.Run("return Infinity;")));

            Assert.True(double.IsNegativeInfinity(Script.Run("return -Infinity;")));
        }

        [Test]
        public void Negate()
        {
            _result = Script.Run(@"
                var a = 100;
                return -a;
            ");

            Assert.AreEqual((MondValue)(-100), _result);
        }

        [Test]
        public void BitNot()
        {
            _result = Script.Run(@"
                var a = 100;
                return ~a;
            ");

            Assert.AreEqual((MondValue)(-101), _result);
        }
    }
}

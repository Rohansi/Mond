﻿using NUnit.Framework;

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

            Assert.True(Script.Run("return 100.35;") == 100.35, "decimal");

            Assert.True(Script.Run("return -100;") == -100, "sign");

            Assert.True(Script.Run("return 10e4;") == 10e4, "exponent");

            Assert.True(Script.Run("return 10e40;") == 10e40, "big exponent");

            Assert.True(Script.Run("return 10e+4;") == 10e+4, "exponent sign +");

            Assert.True(Script.Run("return 10e-4;") == 10e-4, "exponent sign -");

            Assert.True( Script.Run( "return 0xDEADBEEF;" ) == 0xDEADBEEF, "hex number" );
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
    }
}

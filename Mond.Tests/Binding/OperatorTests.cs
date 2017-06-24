using System;
using System.Collections.Generic;
using System.Linq;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public class OperatorTests
    {
        [MondModule]
        public class MyOperators
        {
            [MondOperator("<..>")]
            public static MondValue InclusiveRange(double begin, double end)
            {
                return CreateGenerator(begin, end, true);
            }

            [MondOperator("<...>")]
            public static MondValue ExclusiveRange(double begin, double end)
            {
                return CreateGenerator(begin, end, false);
            }

            [MondOperator("%%")]
            public static double SumOrDouble(MondState state, MondValue range)
            {
                if (range.Type == MondValueType.Number)
                    return range * 2d;

                return range.Enumerate(state).Sum(n => (double)n);
            }

            private static MondValue CreateGenerator(double begin, double end, bool inclusive)
            {
                var actualBegin = Math.Min(begin, end);
                var actualEnd = Math.Max(begin, end);
                var range = MondValue.FromEnumerable(Generate(actualBegin, actualEnd, inclusive));
                range["begin"] = begin;
                range["end"] = end;
                range["inclusive"] = inclusive;

                return range;
            }

            private static IEnumerable<MondValue> Generate(double begin, double end, bool inclusive)
            {
                var i = begin;
                for (; i < end; ++i)
                    yield return i;

                if (inclusive)
                    yield return ++i;
            }
        }

        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState();
            var ops = MondModuleBinder.Bind<MyOperators>(_state);
            foreach( var pair in ops.Object )
                this._state[pair.Key] = pair.Value;
        }

        [Test]
        public void Unary()
        {
            var result = _state.Run("return %% 5;");

            Assert.True(result == 10);
        }

        [Test]
        public void Binary()
        {
            var result = _state.Run(@"
                return {
                    a: 0 <..> 5,
                    b: 6 <...> 10
                };");

            var a = result["a"];
            var b = result["b"];

            Assert.True(a["inclusive"] == true && a["begin"] == 0 && a["end"] == 5);
            Assert.True(b["inclusive"] == false && b["begin"] == 6 && b["end"] == 10);
        }

        [Test]
        public void Mixed()
        {
            var result = _state.Run(@"
                return {
                    a: %%(0 <..> 5),
                    b: %%(6 <...> 10)
                };");

            Assert.True(result["a"] == 16);
            Assert.True(result["b"] == 30);
        }

        [Test]
        public void Nesting()
        {
            var result = Script.Run( @"
                fun divrem(x, y) {
                    fun (%%)(a, b) {
                        return {
                            quotient:  a / b,
                            remainder: a % b,
                        };
                    }

                    return x %% y;
                }

                return divrem(5, 2);
            " );

            Assert.AreEqual( 2, (int)result["quotient"] );
            Assert.AreEqual( 1, (int)result["remainder"] );

            // ensure the nested operator is not visible from the outer scopes
            result = Script.Run( @"
                fun test() {
                    fun (%%)( a, b ) {}
                }

                return op_At == undefined ? global.op_At : op_At;  
            " );

            Assert.AreEqual( MondValue.Undefined, result );
        }

        [Test]
        public void Decorated()
        {
            var result = Script.Run( @"
                fun double(fn) {
                    return fun(...args) -> fn(...args) * 2;
                }

                @double
                fun (^^)( x ) -> x ** 2;

                return ^^10;
            " );
            
            Assert.AreEqual( 200, (int)result );
        }
    }
}

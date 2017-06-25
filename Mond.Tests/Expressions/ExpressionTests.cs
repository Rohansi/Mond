using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ExpressionTests
    {
        [Test]
        public void OrderOfOperations()
        {
            var result = Script.Run(@"
                return 3 + 4 ** 4 * 2 / (1 - 5) % 3;
            ");

            Assert.True((MondValue)1, result);
        }

        [Test]
        public void BinaryOrderOfOperations()
        {
            var result = Script.Run(@"
                return 4 | 2 * 6 << 4 ^ 6 >> 2 + 2 & 4;
            ");

            Assert.AreEqual((MondValue)196, result);
        }

        [Test]
        [TestCase(15, 3)]
        [TestCase(5, 9)]
        public void Ternary(int input, int expected)
        {
            var state = Script.Load(@"
                global.test = fun (n) {
                    return n >= 10 ? 3 : 9;
                };
            ");

            var func = state["test"];
            Assert.AreEqual((MondValue)expected, state.Call(func, input));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        [TestCase(3, 4)]
        [TestCase(4, 5)]
        public void NestedTernary(int input, int expected)
        {
            var state = Script.Load(@"
                global.test = fun (n) {
                    return n == 1 ? 2 :
                           n == 2 ? 3 :
                           n == 3 ? 4 :
                           n == 4 ? 5 : 1;
                };
            ");

            var func = state["test"];
            Assert.AreEqual((MondValue)expected, state.Call(func, input));
        }

        [Test]
        public void ConditionalOr()
        {
            var result = Script.Run(@"
                var result = '';

                fun test(val, str) {
                    result += str;
                    return val;
                }
            
                if (test(true, 'a') || test(true, 'b'))
                    result += '!';

                if (test(false, 'A') || test(true, 'B'))
                    result += '!';

                return result;
            ");

            Assert.AreEqual((MondValue)"a!AB!", result);
        }

        [Test]
        public void ConditionalAnd()
        {
            var result = Script.Run(@"
                var result = '';

                fun test(val, str) {
                    result += str;
                    return val;
                }
            
                if (test(true, 'a') && test(true, 'b'))
                    result += '!';

                if (test(false, 'A') && test(true, 'B'))
                    result += '!';

                return result;
            ");

            Assert.AreEqual((MondValue)"ab!A", result);
        }

        [Test]
        public void ConditionalNot()
        {
            var result = Script.Run(@"
                var a = true;
                return ! ! !a;
            ");

            Assert.AreEqual(MondValue.False, result);
        }

        [Test]
        public void Pipeline()
        {
            var result = Script.Run(@"
                fun test(a, b) -> a + b;

                return 123 |> test(1);
            ");

            Assert.AreEqual((MondValue)124, result);
        }

        [Test]
        public void PipelineMustPointToCall()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 |> 1;
            "));
        }

        [Test]
        [TestCase(84, false)]
        [TestCase(14, true)]
        public void InArray(int input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var array = [ 0, 7, 14, 21, 42 ];
                return fun(x) -> x in array;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }

        [Test]
        [TestCase("abc", false)]
        [TestCase("bar", true)]
        public void InObject(string input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var object = {
                    foo: 123,
                    bar: 456,
                    baz: 789
                };
                return fun(x) -> x in object;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }

        [Test]
        [TestCase("cd", true)]
        [TestCase("xy", false)]
        public void InString(string input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var string = 'abcdef';
                return fun(x) -> x in string;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }
        
        [Test]
        [TestCase(84, true)]
        [TestCase(14, false)]
        public void NotInArray(int input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var array = [ 0, 7, 14, 21, 42 ];
                return fun(x) -> x !in array;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }

        [Test]
        [TestCase("abc", true)]
        [TestCase("bar", false)]
        public void NotInObject(string input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var object = {
                    foo: 123,
                    bar: 456,
                    baz: 789
                };
                return fun(x) -> x !in object;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }

        [Test]
        [TestCase("cd", false)]
        [TestCase("xy", true)]
        public void NotInString(string input, bool expected)
        {
            var result = Script.Run(out var state, @"
                var string = 'abcdef';
                return fun(x) -> x !in string;
            ");

            Assert.AreEqual((MondValue)expected, state.Call(result, input));
        }

        [Test]
        public void NestedOperator()
        {
            var result = Script.Run(@"
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
            ");

            Assert.AreEqual(2, (int)result["quotient"]);
            Assert.AreEqual(1, (int)result["remainder"]);

            // ensure the nested operator is not visible from the outer scopes
            result = Script.Run(@"
                fun test() {
                    fun (%%)(a, b) {}
                }

                return (%%) == undefined ? global[(%%)] : (%%);
            ");

            Assert.AreEqual(MondValue.Undefined, result);
        }

        [Test]
        public void DecoratedOperator()
        {
            var result = Script.Run(@"
                fun double(fn) {
                    return fun(...args) -> fn(...args) * 2;
                }

                @double
                fun (^^)(x) -> x ** 2;

                return ^^10;
            ");

            Assert.AreEqual(200, (int)result);
        }

        [Test]
        public void OperatorReference()
        {
            var result = Script.Run(@"
                fun (#)(x) -> x.length();

                return (#).getName();
            ");

            Assert.AreEqual("op_Hash", result.ToString());
        }
    }
}

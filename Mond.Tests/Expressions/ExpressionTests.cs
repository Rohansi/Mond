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

            Assert.True(result == 1);

            result = Script.Run(@"
                return 4 | 2 * 6 << 4 ^ 6 >> 2 + 2 & 4;
            ");

            Assert.True(result == 196);
        }

        [Test]
        public void Ternary()
        {
            var state = Script.Load(@"
                global.test = fun (n) {
                    return n >= 10 ? 3 : 9;
                };
            ");

            var func = state["test"];

            Assert.True(state.Call(func, 15) == 3);

            Assert.True(state.Call(func, 5) == 9);
        }

        [Test]
        public void NestedTernary()
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

            Assert.True(state.Call(func, 0) == 1);

            Assert.True(state.Call(func, 1) == 2);

            Assert.True(state.Call(func, 2) == 3);

            Assert.True(state.Call(func, 3) == 4);

            Assert.True(state.Call(func, 4) == 5);
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

            Assert.True(result == "a!AB!");
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

            Assert.True(result == "ab!A");
        }

        [Test]
        public void ConditionalNot()
        {
            var result = Script.Run(@"
                var a = true;
                return !a;
            ");

            Assert.True(result == false);

            // TODO: more cases
        }

        [Test]
        public void Pipeline()
        {
            var result = Script.Run(@"
                fun test(a, b) -> a + b;

                return 123 |> test(1);
            ");

            Assert.True(result == 124);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 |> 1;
            "), "right side of pipeline must be function");
        }

        [Test]
        public void In()
        {
            var result = Script.Run(@"
                return 84 in [ 0, 7, 14, 21, 42 ];
            ");

            Assert.True(result == false);

            result = Script.Run(@"
                var object = {
                    foo: 123,
                    bar: 456,
                };

                var key = 'baz' in object ? 'baz' : 'foo';
                return object[key];
            ");

            Assert.True(result == 123);

            result = Script.Run(@"
                var string = 'abcdef';
                return 'ab' in string && 'cd' in string;
            ");

            Assert.True(result == true);
        }

        [Test]
        public void NotIn()
        {
            var result = Script.Run(@"
                return 84 !in [ 0, 7, 14, 21, 42 ];
            ");

            Assert.True(result == true);

            result = Script.Run(@"
                var object = {
                    foo: 123,
                    bar: 456,
                };

                var key = 'baz' !in object ? 'foo' : 'baz';
                return object[key];
            ");

            Assert.True(result == 123);

            result = Script.Run(@"
                var string = 'abcdef';
                return 'gh' !in string && 'ij' !in string;
            ");

            Assert.True(result == true);
        }
    }
}

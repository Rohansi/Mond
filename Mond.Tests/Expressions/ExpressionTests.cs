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
                return 3 + 4 * 2 / (1 - 5) % 3;
            ");

            Assert.True(result == 1);
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
        public void LogicalOr()
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
        public void LogicalAnd()
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
        public void LogicalNot()
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
    }
}

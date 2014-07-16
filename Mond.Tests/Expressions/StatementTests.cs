using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class StatementTests
    {
        [Test]
        public void If()
        {
            var state = Script.Load(@"
                test = fun (x) {

                    if (x < 0) {
                        return 1;
                    } else if (x >= 10) {
                        return 2;
                    } else {
                        return 3;
                    }

                    return 4;
                };
            ");

            var test = state["test"];

            Assert.True(state.Call(test, -3) == 1);

            Assert.True(state.Call(test, 12) == 2);

            Assert.True(state.Call(test, 5) == 3);
        }

        [Test]
        public void Switch()
        {
            var state = Script.Load(@"
                test = fun (x) {
                    
                    switch (x) {
                        case 1:         return 1;
                        case 2:         return 2;
                        case null:      return 3;
                        case 4:         return 4;
                        case 'beep':    return 5;
                        default:        return 6;
                    }

                    return 7;
                };
            ");

            var test = state["test"];

            Assert.True(state.Call(test, 1) == 1);

            Assert.True(state.Call(test, 2) == 2);

            Assert.True(state.Call(test, MondValue.Null) == 3);

            Assert.True(state.Call(test, 4) == 4);

            Assert.True(state.Call(test, "beep") == 5);

            Assert.True(state.Call(test, MondValue.Undefined) == 6);
        }
    }
}

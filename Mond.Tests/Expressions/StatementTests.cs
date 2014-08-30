using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class StatementTests
    {
        [Test]
        public void Scope()
        {
            var result = Script.Run(@"
                {
                    var a = 100;
                }

                {
                    var a;
                    return a;
                }
            ");

            Assert.True(result == MondValue.Undefined);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                {
                    var a = 1;
                }

                return a;
            "));

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var a = 1;

                {
                    var a = 2;
                }
            "));
        }

        [Test]
        public void Constant()
        {
            var result = Script.Run(@"
                const a = 100;
                return a;
            ");

            Assert.True(result == 100);
            
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                a = 123;
            "));

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                return a++;
            "));

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                return ++a;
            "));
        }

        [Test]
        public void If()
        {
            var state = Script.Load(@"
                global.test = fun (x) {

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
                global.test = fun (x) {
                    
                    switch (x) {
                        case 1:         return 1;
                        case 2:         return 2;
                        case 3:         return 3;

                        case 4:
                        case 5:
                            if (x == 5)
                                break;
                            
                            return 4;
                            
                        case 'beep':    return 6;

                        case true:      return 7;
                        case false:     return 8;
                        case null:      return 9;
                        case undefined: return 10;

                        default:        return 11;
                    }

                    return 5;
                };
            ");

            var test = state["test"];

            Assert.True(state.Call(test, 1) == 1);

            Assert.True(state.Call(test, 2) == 2);

            Assert.True(state.Call(test, 3) == 3);

            Assert.True(state.Call(test, 4) == 4);

            Assert.True(state.Call(test, 5) == 5);

            Assert.True(state.Call(test, "beep") == 6);

            Assert.True(state.Call(test, MondValue.True) == 7);

            Assert.True(state.Call(test, MondValue.False) == 8);

            Assert.True(state.Call(test, MondValue.Null) == 9);

            Assert.True(state.Call(test, MondValue.Undefined) == 10);

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                switch (1) { var }
            "));
        }
    }
}

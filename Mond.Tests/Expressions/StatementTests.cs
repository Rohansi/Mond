using System.Collections.Generic;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class StatementTests
    {
        [Test]
        public void ScopeNameReuse()
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

            Assert.AreEqual(MondValue.Undefined, result);
        }

        [Test]
        public void ScopeReferenceOutside()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                {
                    var a = 1;
                }

                return a;
            "));
        }

        [Test]
        public void ScopeDuplicate()
        {
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

            Assert.AreEqual((MondValue)100, result);
        }

        [Test]
        public void ConstantNoValue()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a;
            "));
        }

        [Test]
        public void ConstantAssign()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                a = 123;
            "));
        }

        [Test]
        public void ConstantAssignPostfix()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                return a++;
            "));
        }

        [Test]
        public void ConstantAssignPrefix()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                const a = 100;
                return ++a;
            "));
        }

        public static IEnumerable<TestCaseData> IfData { get; } = new[]
        {
            new TestCaseData((MondValue)(-3), (MondValue)1),
            new TestCaseData((MondValue)12, (MondValue)2),
            new TestCaseData((MondValue)5, (MondValue)3),
        };

        [Test]
        [TestCaseSource(nameof(IfData))]
        public void If(MondValue input, MondValue expected)
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
            Assert.AreEqual(expected, state.Call(test, input));
        }

        [Test]
        [TestCaseSource(nameof(IfData))]
        public void IfNoBraces(MondValue input, MondValue expected)
        {
            var state = Script.Load(@"
                global.test = fun (x) {

                    if (x < 0) 
                        return 1;
                    else if (x >= 10)
                        return 2;
                    else
                        return 3;

                    return 4;
                };
            ");

            var test = state["test"];
            Assert.AreEqual(expected, state.Call(test, input));
        }

        public static IEnumerable<TestCaseData> SwitchData { get; } = new[]
        {
            new TestCaseData((MondValue)1, (MondValue)1),
            new TestCaseData((MondValue)2, (MondValue)2),
            new TestCaseData((MondValue)3, (MondValue)3),
            new TestCaseData((MondValue)4, (MondValue)4),
            new TestCaseData((MondValue)5, (MondValue)5),
            new TestCaseData((MondValue)"beep", (MondValue)6),
            new TestCaseData(MondValue.True, (MondValue)7),
            new TestCaseData(MondValue.False, (MondValue)8),
            new TestCaseData(MondValue.Null, (MondValue)9),
            new TestCaseData(MondValue.Undefined, (MondValue)10),
            new TestCaseData((MondValue)11, (MondValue)11),
        };

        [Test]
        [TestCaseSource(nameof(SwitchData))]
        public void Switch(MondValue input, MondValue expected)
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
            Assert.AreEqual(expected, state.Call(test, input));
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 1)]
        public void SwitchMixedDefault(int input, int expected)
        {
            var state = Script.Load(@"
                global.test = fun (x) {
                    
                    switch (x) {
                        case 1:
                        default:
                            return 1;

                        case 2:
                            return 2;
                    }

                    return 0;
                };
            ");

            var test = state["test"];
            Assert.AreEqual((MondValue)expected, state.Call(test, input));
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 0)]
        public void SwitchNoDefault(int input, int expected)
        {
            var state = Script.Load(@"
                global.test = fun (x) {
                    
                    switch (x) {
                        case 1:
                            return 1;
                        case 2:
                            return 2;
                    }

                    return 0;
                };
            ");

            var test = state["test"];
            Assert.AreEqual((MondValue)expected, state.Call(test, input));
        }

        [Test]
        public void SwitchNotACase()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                switch (1) { var }
            "));
        }

        [Test]
        public void SwitchDuplicateLabel()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                switch (1) {
                    case 1:
                    case 1:
                        return 0;
                }
            "));
        }

        [Test]
        public void SwitchDuplicateDefault()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                switch (1) {
                    default:
                    default:
                        return 0;
                }
            "));
        }

        [Test]
        public void SwitchDuplicateDefaultBlocks()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                switch (1) {
                    default:
                        return 0;
                    default:
                        return 1;
                }
            "));
        }
    }
}

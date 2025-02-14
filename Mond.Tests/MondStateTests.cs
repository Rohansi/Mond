using NUnit.Framework;
using System;

namespace Mond.Tests
{
    [TestFixture]
    public class MondStateTests
    {
        [Test]
        public void MultiplePrograms()
        {
            const string source1 = @"
                global.hello = fun (x) {
                    return 'hi ' + x;
                };

                global.a = global.hello('nerd');
            ";

            const string source2 = @"
                global.b = global.hello('brian');
            ";

            var state = Script.Load(source1, source2);

            var result1 = state["a"];
            var result2 = state["b"];

            Assert.True(result1 == "hi nerd");
            Assert.True(result2 == "hi brian");
        }

        [Test]
        public void NativeFunction()
        {
            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };

            state["function"] = new MondFunction((_, args) => args[0]);

            var result = state.Run(@"
                return global.function('arg');
            ");

            Assert.True(result == "arg");
        }

        [Test]
        [TestCase("runtime", false)]
        [TestCase("generic", false)]
        [TestCase("indirect", true)]
        public void NativeTransitions(string testName, bool hasNativeTransition)
        {
            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };

            state["runtimeEx"] = MondValue.Function((_, args) => { throw new MondRuntimeException("runtime"); });
            state["genericEx"] = MondValue.Function((_, args) => { throw new Exception("generic"); });
            state["call"] = MondValue.Function((_, args) => state.Call(args[0]));

            const string programTemplate = @"
                return {{
                    runtime: () -> global.runtimeEx(),
                    generic: () -> global.genericEx(),
                    indirect: () -> global.call(() -> global.runtimeEx())
                }}.{0}();
            ";

            var program = string.Format(programTemplate, testName);
            var exception = Assert.Throws<MondRuntimeException>(() => state.Run(program));
            Assert.AreEqual(hasNativeTransition, exception.ToString().Contains("[... native ...]"), testName);
        }
    }
}

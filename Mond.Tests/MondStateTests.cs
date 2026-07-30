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

            Assert.That(result1 == "hi nerd", Is.True);
            Assert.That(result2 == "hi brian", Is.True);
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

            Assert.That(result == "arg", Is.True);
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
            Assert.That(exception.ToString().Contains("[... native ...]"), Is.EqualTo(hasNativeTransition), testName);
        }
    }
}

using NUnit.Framework;

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
            var state = new MondState();

            state["function"] = new MondFunction((_, args) => args[0]);

            var result = state.Run(@"
                return global.function('arg');
            ");

            Assert.True(result == "arg");
        }

        [Test]
        public void NativeInstanceFunction()
        {
            var state = new MondState();

            state["value"] = 123;
            state["function"] = new MondInstanceFunction((_, instance, arguments) => instance[arguments[0]]);

            var result = state.Run(@"
                return global.function('value');
            ");

            Assert.True(result == 123);
        }
    }
}

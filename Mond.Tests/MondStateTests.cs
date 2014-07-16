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
                hello = fun (x) {
                    return 'hi ' + x;
                };

                a = hello('nerd');
            ";

            const string source2 = @"
                b = hello('brian');
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

            var program = MondProgram.Compile(@"
                return function('arg');
            ");

            var result = state.Load(program);

            Assert.True(result == "arg");
        }

        [Test]
        public void NativeInstanceFunction()
        {
            var state = new MondState();

            state["value"] = 123;
            state["function"] = new MondInstanceFunction((_, instance, arguments) => instance[arguments[0]]);

            var program = MondProgram.Compile(@"
                return function('value');
            ");

            var result = state.Load(program);

            Assert.True(result == 123);
        }
    }
}

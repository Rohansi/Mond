using NUnit.Framework;

namespace Mond.Tests
{
    [TestFixture]
    public class MondStateTests
    {
        [Test]
        public void MultiplePrograms()
        {
            var state = new MondState();

            var prog1 = MondProgram.Compile(@"
                hello = fun (x) {
                    return 'hi ' + x;
                };

                return hello('nerd');
            ");

            var prog2 = MondProgram.Compile(@"
                return hello('brian');
            ");

            var result1 = state.Load(prog1);
            var result2 = state.Load(prog2);

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
            state["function"] = new MondInstanceFunction((mondState, instance, arguments) => instance[arguments[0]]);

            var program = MondProgram.Compile(@"
                return function('value');
            ");

            var result = state.Load(program);

            Assert.True(result == 123);
        }
    }
}

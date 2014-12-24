using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public class ClassTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState();
            _state["Person"] = MondClassBinder.Bind<Person>();

            var program = MondProgram.Compile(@"
                global.brian = global.Person('Brian');
            ");

            _state.Load(program);
        }

        [Test]
        public void Methods()
        {
            Assert.True(_state.Run(@"
                return global.brian.generateGreeting();
            ") == "hello Brian!");

            Assert.True(_state.Run(@"
                global.brian.changeInstance();
                return global.brian.test;
            ") == 100);

            Assert.True(_state.Run(@"
                global.brian.changeState();
                return global.test;
            ") == 100);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.brian.UnmarkedFunction();
            "));

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.brian.staticFunction();
            "));
        }

        [Test]
        public void Properties()
        {
            Assert.True(_state.Run(@"
                return global.brian.getName();
            ") == "Brian");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.brian.setName('not brian');
            "));

            Assert.True(_state.Run(@"
                return global.brian.getAge();
            ") == -1);

            Assert.True(_state.Run(@"
                global.brian.setAge(4);
                return global.brian.getAge();
            ") == 4);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.brian.setUnmarkedProperty();
            "));

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.brian.setUnmarkedProperty(true);
            "));
        }

        [Test]
        public void Constructor()
        {
            Assert.True(_state.Run("return global.brian.test;") == 123);

            Assert.Throws<MondBindingException>(() => MondClassBinder.Bind<NoConstructor>());

            Assert.DoesNotThrow(() => MondClassBinder.Bind<MultipleConstructors>());
        }

        [Test]
        public void Duplicates()
        {
            Assert.DoesNotThrow(() => MondClassBinder.Bind<TestDuplicate>());
        }

        [MondClass]
        public class Person
        {
            [MondConstructor]
            public Person([MondInstance] MondValue instance, string name)
            {
                Name = name;
                Age = -1;

                instance["test"] = 123;
            }

            [MondFunction]
            public string Name { get; private set; }

            [MondFunction]
            public int Age { get; set; }

            [MondFunction("generateGreeting")]
            public string GenerateGreeting()
            {
                return string.Format("hello {0}!", Name);
            }

            [MondFunction("changeInstance")]
            public void ChangeInstance([MondInstance] MondValue instance)
            {
                instance["test"] = 100;
            }

            [MondFunction("changeState")]
            public void ChangeState(MondState state)
            {
                state["test"] = 100;
            }

            public int UnmarkedProperty { get; set; }

            public bool UnmarkedFunction()
            {
                return true;
            }

            [MondFunction("staticFunction")]
            public static bool StaticFunction()
            {
                return true;
            }
        }

        [MondClass]
        public class NoConstructor
        {
            
        }

        [MondClass]
        public class MultipleConstructors
        {
            public int N;

            [MondConstructor]
            public MultipleConstructors()
            {
                N = 0;
            }

            [MondConstructor]
            public MultipleConstructors(int n)
            {
                N = n;
            }
        }

        [MondClass]
        public class TestDuplicate
        {
            [MondConstructor]
            public TestDuplicate()
            {
                
            }

            [MondFunction]
            public void Method()
            {

            }

            [MondFunction]
            public void Method(int n)
            {

            }
        }
    }
}

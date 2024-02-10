using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public partial class ClassTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState
            {
                Libraries =
                {
                    new Person.Library(),
                },
            };

            _state.Run(@"
                global.brian = Person('Brian');
            ");
        }

        [Test]
        public void Methods()
        {
            Assert.True(_state.Run(@"
                return global.brian.generateGreeting();
            ") == "hello Brian!");

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
            var type = _state.Run("return global.brian.getType();");
            Assert.True(type == "object");
        }

        [MondClass]
        public partial class Person
        {
            [MondConstructor]
            public Person(string name)
            {
                Name = name;
                Age = -1;
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
        public partial class NoConstructor
        {
            
        }

        [MondClass]
        public partial class MultipleConstructors
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
        public partial class TestDuplicate
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

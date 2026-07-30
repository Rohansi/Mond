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
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
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
            Assert.That(_state.Run(@"
                return global.brian.generateGreeting();
            ") == "hello Brian!", Is.True);

            Assert.That(_state.Run(@"
                global.brian.changeState();
                return global.test;
            ") == 100, Is.True);

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
            Assert.That(_state.Run(@"
                return global.brian.getName();
            ") == "Brian", Is.True);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.brian.setName('not brian');
            "));

            Assert.That(_state.Run(@"
                return global.brian.getAge();
            ") == -1, Is.True);

            Assert.That(_state.Run(@"
                global.brian.setAge(4);
                return global.brian.getAge();
            ") == 4, Is.True);

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
            Assert.That(type == "object", Is.True);
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

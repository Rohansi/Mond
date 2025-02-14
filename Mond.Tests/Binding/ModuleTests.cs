using System;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public partial class ModuleTests
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
            };

            foreach (var t in new Test.Library().GetDefinitions(_state))
            {
                _state[t.Key] = t.Value;
            }
        }

        [Test]
        public void Methods()
        {
            Assert.True(_state.Run(@"
                return global.Test.sin(10);
            ") == Math.Sin(10));

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.Test.unmarkedFunction();
            "));

            Assert.DoesNotThrow(() => _state.Run(@"
                return global.Test.instanceFunction();
            "));
        }

        [Test]
        public void Properties()
        {
            Assert.True(_state.Run(@"
                global.Test.setProperty('test');
                return global.Test.getProperty();
            "));

            Assert.True(_state.Run(@"
                return global.Test.getPropertyPrivateSet();
            ") == true);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.Test.setPropertyPrivateSet(false);
            "));

            Assert.True(_state.Run(@"
                return global.Test.getPropertyNoSet();
            ") == 7);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.Test.setPropertyNoSet(10);
            "));

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.Test.getUnmarkedProperty();
            "));
        }

        [MondModule]
        public partial class Test
        {
            static Test()
            {
                PropertyPrivateSet = true;
            }

            [MondFunction]
            public static string Property { get; set; }

            [MondFunction]
            public static bool PropertyPrivateSet { get; private set; }

            [MondFunction]
            public static int PropertyNoSet { get { return 7; } }

            [MondFunction]
            public static double Sin(double a)
            {
                return Math.Sin(a);
            }

            public static bool UnmarkedProperty { get; set; }

            public static int UnmarkedFunction()
            {
                return 0;
            }

            [MondFunction]
            public int InstanceFunction()
            {
                return 0;
            }
        }

        [MondModule]
        public partial class TestDuplicate
        {
            [MondFunction]
            public static void Method()
            {
                
            }

            [MondFunction]
            public static void Method(int n)
            {
                
            }
        }
    }
}

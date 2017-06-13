using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public class FunctionOverloadingTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState();
            _state["Ov"] = MondModuleBinder.Bind<OverloadedModule>(_state);
        }

        [Test]
        public void Resolution()
        {
            Assert.True(_state.Run("return global.Ov.test();") == 0, "0.0");

            Assert.True(_state.Run("return global.Ov.test(true);") == 0, "0.1");
            Assert.True(_state.Run("return global.Ov.test(false);") == 0, "0.2");

            Assert.True(_state.Run("return global.Ov.test('hi');") == 1, "1.0");

            Assert.True(_state.Run("return global.Ov.test(10);") == 2, "2.0");
            Assert.True(_state.Run("return global.Ov.test(10, 20);") == 2, "2.1");

            Assert.True(_state.Run("return global.Ov.test(undefined, 20);") == 3, "3.0");
            Assert.True(_state.Run("return global.Ov.test(null, 20);") == 3, "3.1");
            Assert.True(_state.Run("return global.Ov.test([], 20);") == 3, "3.2");

            Assert.True(_state.Run("return global.Ov.test('hi', 20);") == 4, "4.0");
            Assert.True(_state.Run("return global.Ov.test('hi', 20, 30);") == 4, "4.1");

            Assert.True(_state.Run("return global.Ov.test(10, 20, 30);") == 5, "5.0");
            Assert.True(_state.Run("return global.Ov.test(10, 20, 30, 40);") == 5, "5.1");

            Assert.True(_state.Run("return global.Ov.test([], 'hi');") == 6, "6.0");
            Assert.True(_state.Run("return global.Ov.test('hi', 'hi');") == 6, "6.1");
            Assert.True(_state.Run("return global.Ov.test('hi', 20, 'hi');") == 6, "6.2");
        }

        [Test]
        public void HiddenOverloads()
        {
            Assert.Throws<MondBindingException>(() => MondModuleBinder.Bind<HiddenOverloadsModule>(_state));
        }

        [MondModule]
        public class OverloadedModule
        {
            [MondFunction]
            public static int Test(bool x = true)
            {
                return 0;
            }

            [MondFunction]
            public static int Test(string x)
            {
                return 1;
            }

            [MondFunction]
            public static int Test(int x, int y = 1)
            {
                return 2;
            }

            [MondFunction]
            public static int Test(MondValue x, int y)
            {
                return 3;
            }

            [MondFunction]
            public static int Test(string x, int y, int z = 2)
            {
                return 4;
            }

            [MondFunction]
            public static int Test(int x, int y, params MondValue[] args)
            {
                return 5;
            }

            [MondFunction]
            public static int Test(params MondValue[] args)
            {
                return 6;
            }
        }

        [MondModule]
        public class HiddenOverloadsModule
        {
            [MondFunction]
            public static int Add(int x, int y)
            {
                return x + y;
            }

            [MondFunction]
            public static double Add(double x, double y)
            {
                return x + y;
            }
        }
    }
}

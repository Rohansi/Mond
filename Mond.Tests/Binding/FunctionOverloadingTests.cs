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
            _state["Ov"] = MondModuleBinder.Bind<OverloadedModule>();
        }

        [Test]
        public void Resolution()
        {
            Assert.True(_state.Run("return global.Ov.Test();") == 0, "0.0");

            Assert.True(_state.Run("return global.Ov.Test(true);") == 0, "0.1");
            Assert.True(_state.Run("return global.Ov.Test(false);") == 0, "0.2");

            Assert.True(_state.Run("return global.Ov.Test('hi');") == 1, "1.0");

            Assert.True(_state.Run("return global.Ov.Test(10);") == 2, "2.0");
            Assert.True(_state.Run("return global.Ov.Test(10, 20);") == 2, "2.1");

            Assert.True(_state.Run("return global.Ov.Test(undefined, 20);") == 3, "3.0");
            Assert.True(_state.Run("return global.Ov.Test(null, 20);") == 3, "3.1");
            Assert.True(_state.Run("return global.Ov.Test([], 20);") == 3, "3.2");

            Assert.True(_state.Run("return global.Ov.Test('hi', 20);") == 4, "4.0");
            Assert.True(_state.Run("return global.Ov.Test('hi', 20, 30);") == 4, "4.1");

            Assert.True(_state.Run("return global.Ov.Test(10, 20, 30);") == 5, "5.0");
            Assert.True(_state.Run("return global.Ov.Test(10, 20, 30, 40);") == 5, "5.1");

            Assert.True(_state.Run("return global.Ov.Test([], 'hi');") == 6, "6.0");
            Assert.True(_state.Run("return global.Ov.Test('hi', 'hi');") == 6, "6.1");
            Assert.True(_state.Run("return global.Ov.Test('hi', 20, 'hi');") == 6, "6.2");
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
    }
}

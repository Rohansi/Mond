using Mond.VirtualMachine;
using NUnit.Framework;

namespace Mond.Tests
{
    [TestFixture]
    public class MondValueTests
    {
        #region Operators

        private MondValue _left;
        private MondValue _right;

        [Test]
        public void OperatorAdd()
        {
            _left = 10;
            _right = 5;
            Assert.AreEqual(_left + _right, new MondValue(10 + 5));

            _left = "abc";
            _right = "def";
            Assert.AreEqual(_left + _right, new MondValue("abc" + "def"));

            _left = "abc";
            _right = 123;
            Assert.AreEqual(_left + _right, new MondValue("abc" + 123));

            _left = 123;
            _right = "abc";
            Assert.AreEqual(_left + _right, new MondValue(123 + "abc"));

            _left = MondValue.Null;
            _right = "abc";
            Assert.AreEqual(_left + _right, new MondValue("null" + "abc")); // TODO: this shouldn't be allowed imo

            _left = MondValue.Null;
            _right = MondValue.Null;
            Assert.Throws<MondRuntimeException>(() => { _left += _right; });
        }

        [Test]
        public void OperatorSubtract()
        {
            _left = 10;
            _right = 5;
            Assert.AreEqual(_left - _right, new MondValue(10 - 5));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left -= _right; });
        }

        [Test]
        public void OperatorMultiply()
        {
            _left = 10;
            _right = 5;
            Assert.AreEqual(_left * _right, new MondValue(10 * 5));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left *= _right; });
        }

        [Test]
        public void OperatorDivide()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left / _right, new MondValue(10.0 / 7.0));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left /= _right; });
        }

        [Test]
        public void OperatorModulo()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left % _right, new MondValue(10.0 % 7.0));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left %= _right; });
        }

        [Test]
        public void OperatorNegate()
        {
            _left = 10;
            Assert.AreEqual(-_left, new MondValue(-10));

            _left = "10";
            Assert.Throws<MondRuntimeException>(() => { _left = -_left; });
        }

        [Test]
        public void OperatorEqualTo()
        {
            _left = 10;
            _right = 10;
            Assert.True(_left == _right);

            _left = 10;
            _right = 11;
            Assert.False(_left == _right);

            _left = "10";
            _right = 10;
            Assert.False(_left == _right); // no ty

            _left = MondValue.Null;
            _right = MondValue.Undefined;
            Assert.False(_left == _right);

            _left = new MondValue(MondValueType.Object);
            _right = _left;
            Assert.True(_left == _right);
        }

        [Test]
        public void OperatorGreaterThan()
        {
            _left = 11;
            _right = 10;
            Assert.True(_left > _right);

            _left = 10;
            _right = 10;
            Assert.False(_left > _right);

            _left = 9;
            _right = 10;
            Assert.False(_left > _right);

            _left = "a";
            _right = "b";
            Assert.False(_left > _right);

            _left = MondValue.Null;
            _right = 10;
            Assert.Throws<MondRuntimeException>(() => { var a = _left > _right; });
        }

        #endregion

        [Test]
        public void ImplicitBool()
        {
            var value = MondValue.Undefined;
            Assert.False(value);

            value = MondValue.Null;
            Assert.False(value);

            value = MondValue.False;
            Assert.False(value);

            value = MondValue.True;
            Assert.True(value);

            value = new MondValue(MondValueType.Object);
            Assert.True(value);
        }

        [Test]
        public void ObjectFieldIndexer()
        {
            var obj = new MondValue(MondValueType.Object);

            Assert.True(obj["undef"] == MondValue.Undefined);

            Assert.True(obj["prototype"] != MondValue.Undefined);

            obj["test"] = 123;
            Assert.True(obj["test"] == 123);

            obj[123] = "test";
            Assert.True(obj[123] == "test");
        }

        [Test]
        public void ArrayIndexer()
        {
            var array = new MondValue(MondValueType.Array);

            array.ArrayValue.Add("test");
            array.ArrayValue.Add(123);

            Assert.True(array[0] == "test");
            Assert.True(array[1] == 123);

            Assert.Throws<MondRuntimeException>(() => { var a = array[2]; });
        }

        [Test]
        public void ObjectPrototype()
        {
            var prototype = new MondValue(MondValueType.Object);
            var obj = new MondValue(MondValueType.Object);

            obj.Prototype = prototype;
            Assert.True(obj.Prototype.Type == MondValueType.Object);

            prototype["testValue"] = "hello";

            Assert.True(obj["testValue"] == "hello");

            obj.Prototype = MondValue.Null; // override default

            Assert.False(obj["testValue"] == "hello");
        }

        [Test]
        public void WrappedInstanceFunction()
        {
            var obj = new MondValue(MondValueType.Object);

            var func = new MondValue((state, instance, args) => MondValue.Undefined);
            Assert.True(func.FunctionValue.Type == ClosureType.InstanceNative);

            obj["test"] = func;
            var closure = obj["test"];
            Assert.True(closure.FunctionValue.Type == ClosureType.Native);

            Assert.True(new MondState().Call(obj["test"]) == MondValue.Undefined);
        }

        [Test]
        public void UserData()
        {
            const string data = "test";

            var value = new MondValue(MondValueType.Object);
            value.UserData = data;

            Assert.True(ReferenceEquals(data, value.UserData));

            value.UserData = null;

            Assert.True(ReferenceEquals(null, value.UserData));

            Assert.Throws<MondRuntimeException>(() =>
            {
                var a = MondValue.Null.UserData;
            });
        }
    }
}

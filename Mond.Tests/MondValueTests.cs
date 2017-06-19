using System;
using System.Linq;
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
            Assert.AreEqual(_left + _right, MondValue.Number(10 + 5));

            _left = "abc";
            _right = "def";
            Assert.AreEqual(_left + _right, MondValue.String("abc" + "def"));

            _left = "abc";
            _right = 123;
            Assert.AreEqual(_left + _right, MondValue.String("abc" + 123));

            _left = 123;
            _right = "abc";
            Assert.AreEqual(_left + _right, MondValue.String(123 + "abc"));

            _left = MondValue.Null;
            _right = "abc";
            Assert.AreEqual(_left + _right, MondValue.String("null" + "abc")); // TODO: this shouldn't be allowed imo

            _left = MondValue.Null;
            _right = MondValue.Null;
            Assert.Throws<MondRuntimeException>(() => { _left += _right; });
        }

        [Test]
        public void OperatorSubtract()
        {
            _left = 10;
            _right = 5;
            Assert.AreEqual(_left - _right, MondValue.Number(10 - 5));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left -= _right; });
        }

        [Test]
        public void OperatorMultiply()
        {
            _left = 10;
            _right = 5;
            Assert.AreEqual(_left * _right, MondValue.Number(10 * 5));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left *= _right; });
        }

        [Test]
        public void OperatorDivide()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left / _right, MondValue.Number(10.0 / 7.0));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left /= _right; });
        }

        [Test]
        public void OperatorModulo()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left % _right, MondValue.Number(10.0 % 7.0));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left %= _right; });
        }

        [Test]
        public void OperatorPow()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left.Pow(_right), MondValue.Number(Math.Pow(10.0, 7.0)));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left = _left.Pow(_right); });
        }

        [Test]
        public void OperatorLShift()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left.LShift(_right), MondValue.Number(10 << 7));

            _left = 10.0;
            Assert.AreEqual(_left << 7, MondValue.Number(10 << 7));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left = _left.LShift(_right); });

            _left = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left = _left.LShift(_right); });
        }

        [Test]
        public void OperatorRShift()
        {
            _left = 10.0;
            _right = 2.0;
            Assert.AreEqual(_left.RShift(_right), MondValue.Number(10 >> 2));

            _left = 10.0;
            Assert.AreEqual(_left >> 2, MondValue.Number(10 >> 2));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left = _left.RShift(_right); });

            _left = "abc";
            _right = 2.0;
            Assert.Throws<MondRuntimeException>(() => { _left = _left.RShift(_right); });
        }

        [Test]
        public void OperatorAnd()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left & _right, MondValue.Number(10 & 7));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left &= _right; });
        }

        [Test]
        public void OperatorOr()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left | _right, MondValue.Number(10 | 7));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left |= _right; });
        }

        [Test]
        public void OperatorXor()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.AreEqual(_left ^ _right, MondValue.Number(10 ^ 7));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left ^= _right; });
        }

        [Test]
        public void OperatorNegate()
        {
            _left = 10;
            Assert.AreEqual(-_left, MondValue.Number(-10));

            _left = "10";
            Assert.Throws<MondRuntimeException>(() => { _left = -_left; });
        }

        [Test]
        public void OperatorNot()
        {
            _left = 10;
            Assert.AreEqual(~_left, MondValue.Number(~10));

            _left = "10";
            Assert.Throws<MondRuntimeException>(() => { _left = ~_left; });
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

            _left = MondValue.Object();
            _right = _left;
            Assert.True(_left == _right);

            _left = 0;
            _right = MondValue.Null;
            Assert.False(_left == _right, "type check");
        }

        [Test]
        public void OperatorNotEqualTo()
        {
            _left = 10;
            _right = 11;

            Assert.True(_left != _right);
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

        [Test]
        public void OperatorGreaterThanOrEqual()
        {
            _left = 11;
            _right = 10;
            Assert.True(_left >= _right);

            _left = 11;
            _right = 11;
            Assert.True(_left >= _right);

            _left = 11;
            _right = 12;
            Assert.False(_left >= _right);
        }

        [Test]
        public void OperatorLessThan()
        {
            _left = 9;
            _right = 10;
            Assert.True(_left < _right);

            _left = 10;
            _right = 10;
            Assert.False(_left < _right);

            _left = 11;
            _right = 10;
            Assert.False(_left < _right);
        }

        [Test]
        public void OperatorLessThanOrEqual()
        {
            _left = 9;
            _right = 10;
            Assert.True(_left <= _right);

            _left = 10;
            _right = 10;
            Assert.True(_left <= _right);

            _left = 11;
            _right = 10;
            Assert.False(_left <= _right);
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

            value = 0;
            Assert.True(value);

            value = 1;
            Assert.True(value);

            value = double.NaN;
            Assert.False(value);

            value = "hello";
            Assert.True(value);

            value = MondValue.Object();
            Assert.True(value);

            value = MondValue.Array();
            Assert.True(value);

            value = MondValue.Function((state, arguments) => MondValue.Undefined);
            Assert.True(value);
        }

        [Test]
        public void ObjectFieldIndexer()
        {
            var obj = MondValue.Object();

            Assert.True(obj["undef"] == MondValue.Undefined);

            Assert.True(obj["setPrototype"] != MondValue.Undefined);

            obj["test"] = 123;
            Assert.True(obj["test"] == 123);

            obj[123] = "test";
            Assert.True(obj[123] == "test");
        }

        [Test]
        public void ArrayIndexer()
        {
            var array = MondValue.Array();

            array.AsList.Add("test");
            array.AsList.Add(123);

            Assert.True(array[0] == "test");
            Assert.True(array[1] == 123);

            Assert.Throws<MondRuntimeException>(() => { var a = array[2]; });
        }

        [Test]
        public void ObjectPrototype()
        {
            var prototype = MondValue.Object();
            var obj = MondValue.Object();

            obj.Prototype = prototype;
            Assert.True(obj.Prototype.Type == MondValueType.Object);

            prototype["testValue"] = "hello";

            Assert.True(obj["testValue"] == "hello");
            Assert.True(obj["containsKey"].Type == MondValueType.Function);

            obj.Prototype = MondValue.Null; // no prototype

            Assert.True(obj["testValue"] == MondValue.Undefined);
            Assert.True(obj["containsKey"] == MondValue.Undefined);

            obj.Lock();
            Assert.Throws<MondRuntimeException>(() => obj.Prototype = MondValue.Undefined, "modify locked object prototype");
        }

        [Test]
        public void WrappedInstanceFunction()
        {
            var obj = MondValue.Object();

            var func = MondValue.Function((state, instance, args) => MondValue.Undefined);
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

            var value = MondValue.Object();
            value.UserData = data;

            Assert.True(ReferenceEquals(data, value.UserData));

            value.UserData = null;

            Assert.True(ReferenceEquals(null, value.UserData));

            Assert.Throws<MondRuntimeException>(() =>
            {
                var a = MondValue.Null.UserData;
            });
        }

        [Test]
        public void Lock()
        {
            var obj = MondValue.Object();
            var prototype = obj.Prototype;

            Assert.True(obj["getType"].Type == MondValueType.Function, "no getType");

            obj["getType"] = 123;

            Assert.True(prototype["getType"].Type == MondValueType.Function, "set wrong field on locked prototype");
            Assert.True(obj["getType"] == 123, "set on locked prototype");

            Assert.Throws<MondRuntimeException>(() => prototype["getType"] = 123, "set on locked object");

            Assert.True(obj["test"] == MondValue.Undefined);

            obj.Lock();

            Assert.Throws<MondRuntimeException>(() => obj["test"] = 123, "create on locked object");
        }

        [Test]
        public void Contains()
        {
            var arr = MondValue.Array();
            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });

            Assert.True(arr.Contains(3));
            Assert.False(arr.Contains(10));

            var str = MondValue.String("hello world");

            Assert.True(str.Contains("hello"));
            Assert.False(str.Contains("asdf"));

            var obj = MondValue.Object(new MondState());
            obj["__in"] = new MondFunction((state, args) => args[1].Type == MondValueType.Number);

            Assert.True(obj.Contains(3));
            Assert.False(obj.Contains("hello"));

            Assert.Throws<MondRuntimeException>(() => MondValue.False.Contains(0));
        }

        [Test]
        public void Slice()
        {
            var state = new MondState();

            var arr = MondValue.Array();
            var str = MondValue.String("HelloWorld");

            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });

            Assert.True(str.Slice(1, 3, 1).Equals(MondValue.String("ell")));

            Assert.True(arr.Slice().Enumerate(state).SequenceEqual(arr.Enumerate(state)), "clone");

            Assert.True(arr.Slice(step: -1).Enumerate(state).SequenceEqual(new MondValue[] { 5, 4, 3, 2, 1 }), "reverse");

            Assert.True(arr.Slice(1, 3).Enumerate(state).SequenceEqual(new MondValue[] { 2, 3, 4 }), "range");
            Assert.True(arr.Slice(3, 1).Enumerate(state).SequenceEqual(new MondValue[] { 4, 3, 2 }), "reverse range");

            Assert.True(arr.Slice(0, 0).Enumerate(state).SequenceEqual(new MondValue[] { 1 }), "same start and end");

            Assert.True(arr.Slice(-4, -2).Enumerate(state).SequenceEqual(new MondValue[] { 2, 3, 4 }), "negative range");
            Assert.True(arr.Slice(-2, -4).Enumerate(state).SequenceEqual(new MondValue[] { 4, 3, 2 }), "negative range reverse");

            Assert.True(arr.Slice(step: 2).Enumerate(state).SequenceEqual(new MondValue[] { 1, 3, 5 }), "skip");
            Assert.True(arr.Slice(step: -2).Enumerate(state).SequenceEqual(new MondValue[] { 5, 3, 1 }), "skip negative");

            Assert.Throws<MondRuntimeException>(() => arr.Slice(-6, 0, "out of bounds 1"));
            Assert.Throws<MondRuntimeException>(() => arr.Slice(0, 5, "out of bounds 2"));

            Assert.Throws<MondRuntimeException>(() => arr.Slice(step: 0), "invalid step");

            Assert.Throws<MondRuntimeException>(() => arr.Slice(4, 0, 1), "invalid range");
            Assert.Throws<MondRuntimeException>(() => arr.Slice(0, 4, -1), "invalid range negative");

            Assert.Throws<MondRuntimeException>(() => MondValue.Undefined.Slice(), "slice non-array");

            var empty = MondValue.Array();
            Assert.True(!empty.Slice().Enumerate(state).Any(), "clone empty");
        }
    }
}

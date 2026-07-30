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
            Assert.That(MondValue.Number(10 + 5), Is.EqualTo(_left + _right));

            _left = "abc";
            _right = "def";
            Assert.That(MondValue.String("abc" + "def"), Is.EqualTo(_left + _right));

            _left = "abc";
            _right = 123;
            Assert.That(MondValue.String("abc" + 123), Is.EqualTo(_left + _right));

            _left = 123;
            _right = "abc";
            Assert.That(MondValue.String(123 + "abc"), Is.EqualTo(_left + _right));

            _left = MondValue.Null;
            _right = "abc";
            Assert.That(MondValue.String("null" + "abc"), Is.EqualTo(_left + _right)); // TODO: this shouldn't be allowed imo

            _left = MondValue.Null;
            _right = MondValue.Null;
            Assert.Throws<MondRuntimeException>(() => { _left += _right; });
        }

        [Test]
        public void OperatorSubtract()
        {
            _left = 10;
            _right = 5;
            Assert.That(MondValue.Number(10 - 5), Is.EqualTo(_left - _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left -= _right; });
        }

        [Test]
        public void OperatorMultiply()
        {
            _left = 10;
            _right = 5;
            Assert.That(MondValue.Number(10 * 5), Is.EqualTo(_left * _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left *= _right; });
        }

        [Test]
        public void OperatorDivide()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(10.0 / 7.0), Is.EqualTo(_left / _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left /= _right; });
        }

        [Test]
        public void OperatorModulo()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(10.0 % 7.0), Is.EqualTo(_left % _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left %= _right; });
        }

        [Test]
        public void OperatorPow()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(Math.Pow(10.0, 7.0)), Is.EqualTo(_left.Pow(_right)));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left = _left.Pow(_right); });
        }

        [Test]
        public void OperatorLShift()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(10 << 7), Is.EqualTo(_left.LShift(_right)));

            _left = 10.0;
            Assert.That(MondValue.Number(10 << 7), Is.EqualTo(_left << 7));

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
            Assert.That(MondValue.Number(10 >> 2), Is.EqualTo(_left.RShift(_right)));

            _left = 10.0;
            Assert.That(MondValue.Number(10 >> 2), Is.EqualTo(_left >> 2));

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
            Assert.That(MondValue.Number(10 & 7), Is.EqualTo(_left & _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left &= _right; });
        }

        [Test]
        public void OperatorOr()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(10 | 7), Is.EqualTo(_left | _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left |= _right; });
        }

        [Test]
        public void OperatorXor()
        {
            _left = 10.0;
            _right = 7.0;
            Assert.That(MondValue.Number(10 ^ 7), Is.EqualTo(_left ^ _right));

            _left = 123;
            _right = "abc";
            Assert.Throws<MondRuntimeException>(() => { _left ^= _right; });
        }

        [Test]
        public void OperatorNegate()
        {
            _left = 10;
            Assert.That(MondValue.Number(-10), Is.EqualTo(-_left));

            _left = "10";
            Assert.Throws<MondRuntimeException>(() => { _left = -_left; });
        }

        [Test]
        public void OperatorNot()
        {
            _left = 10;
            Assert.That(MondValue.Number(~10), Is.EqualTo(~_left));

            _left = "10";
            Assert.Throws<MondRuntimeException>(() => { _left = ~_left; });
        }

        [Test]
        public void OperatorEqualTo()
        {
            _left = 10;
            _right = 10;
            Assert.That(_left == _right, Is.True);

            _left = 10;
            _right = 11;
            Assert.That(_left == _right, Is.False);

            _left = "10";
            _right = 10;
            Assert.That(_left == _right, Is.False); // no ty

            _left = MondValue.Null;
            _right = MondValue.Undefined;
            Assert.That(_left == _right, Is.False);

            _left = MondValue.Object();
            _right = _left;
            Assert.That(_left == _right, Is.True);

            _left = 0;
            _right = MondValue.Null;
            Assert.That(_left == _right, Is.False, "type check");
        }

        [Test]
        public void OperatorNotEqualTo()
        {
            _left = 10;
            _right = 11;

            Assert.That(_left != _right, Is.True);
        }

        [Test]
        public void OperatorGreaterThan()
        {
            _left = 11;
            _right = 10;
            Assert.That(_left > _right, Is.True);

            _left = 10;
            _right = 10;
            Assert.That(_left > _right, Is.False);

            _left = 9;
            _right = 10;
            Assert.That(_left > _right, Is.False);

            _left = "a";
            _right = "b";
            Assert.That(_left > _right, Is.False);

            _left = MondValue.Null;
            _right = 10;
            Assert.Throws<MondRuntimeException>(() => { var a = _left > _right; });
        }

        [Test]
        public void OperatorGreaterThanOrEqual()
        {
            _left = 11;
            _right = 10;
            Assert.That(_left >= _right, Is.True);

            _left = 11;
            _right = 11;
            Assert.That(_left >= _right, Is.True);

            _left = 11;
            _right = 12;
            Assert.That(_left >= _right, Is.False);
        }

        [Test]
        public void OperatorLessThan()
        {
            _left = 9;
            _right = 10;
            Assert.That(_left < _right, Is.True);

            _left = 10;
            _right = 10;
            Assert.That(_left < _right, Is.False);

            _left = 11;
            _right = 10;
            Assert.That(_left < _right, Is.False);
        }

        [Test]
        public void OperatorLessThanOrEqual()
        {
            _left = 9;
            _right = 10;
            Assert.That(_left <= _right, Is.True);

            _left = 10;
            _right = 10;
            Assert.That(_left <= _right, Is.True);

            _left = 11;
            _right = 10;
            Assert.That(_left <= _right, Is.False);
        }

        #endregion

        [Test]
        public void ImplicitBool()
        {
            var value = MondValue.Undefined;
            Assert.That<bool>(value, Is.False);

            value = MondValue.Null;
            Assert.That<bool>(value, Is.False);

            value = MondValue.False;
            Assert.That<bool>(value, Is.False);

            value = MondValue.True;
            Assert.That<bool>(value, Is.True);

            value = 0;
            Assert.That<bool>(value, Is.True);

            value = 1;
            Assert.That<bool>(value, Is.True);

            value = double.NaN;
            Assert.That<bool>(value, Is.False);

            value = "hello";
            Assert.That<bool>(value, Is.True);

            value = MondValue.Object();
            Assert.That<bool>(value, Is.True);

            value = MondValue.Array();
            Assert.That<bool>(value, Is.True);

            value = MondValue.Function((state, arguments) => MondValue.Undefined);
            Assert.That<bool>(value, Is.True);
        }

        [Test]
        public void ObjectFieldIndexer()
        {
            var obj = MondValue.Object();

            Assert.That(obj["undef"] == MondValue.Undefined, Is.True);

            Assert.That(obj["setPrototype"] != MondValue.Undefined, Is.True);

            obj["test"] = 123;
            Assert.That(obj["test"] == 123, Is.True);

            obj[123] = "test";
            Assert.That(obj[123] == "test", Is.True);
        }

        [Test]
        public void ArrayIndexer()
        {
            var array = MondValue.Array();

            array.AsList.Add("test");
            array.AsList.Add(123);

            Assert.That(array[0] == "test", Is.True);
            Assert.That(array[1] == 123, Is.True);

            Assert.Throws<MondRuntimeException>(() => { var a = array[2]; });
        }

        [Test]
        public void ObjectPrototype()
        {
            var prototype = MondValue.Object();
            var obj = MondValue.Object();

            obj.Prototype = prototype;
            Assert.That(obj.Prototype.Type == MondValueType.Object, Is.True);

            prototype["testValue"] = "hello";

            Assert.That(obj["testValue"] == "hello", Is.True);
            Assert.That(obj["containsKey"].Type == MondValueType.Function, Is.True);

            obj.Prototype = MondValue.Null; // no prototype

            Assert.That(obj["testValue"] == MondValue.Undefined, Is.True);
            Assert.That(obj["containsKey"] == MondValue.Undefined, Is.True);

            obj.Lock();
            Assert.Throws<MondRuntimeException>(() => obj.Prototype = MondValue.Undefined, "modify locked object prototype");
        }

        [Test]
        public void UserData()
        {
            const string data = "test";

            var value = MondValue.Object();
            value.UserData = data;

            Assert.That(ReferenceEquals(data, value.UserData), Is.True);

            value.UserData = null;

            Assert.That(ReferenceEquals(null, value.UserData), Is.True);

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

            Assert.That(obj["getType"].Type == MondValueType.Function, Is.True, "no getType");

            obj["getType"] = 123;

            Assert.That(prototype["getType"].Type == MondValueType.Function, Is.True, "set wrong field on locked prototype");
            Assert.That(obj["getType"] == 123, Is.True, "set on locked prototype");

            Assert.Throws<MondRuntimeException>(() => prototype["getType"] = 123, "set on locked object");

            Assert.That(obj["test"] == MondValue.Undefined, Is.True);

            obj.Lock();

            Assert.Throws<MondRuntimeException>(() => obj["test"] = 123, "create on locked object");
        }

        [Test]
        public void Contains()
        {
            var arr = MondValue.Array();
            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });

            Assert.That(arr.Contains(3), Is.True);
            Assert.That(arr.Contains(10), Is.False);

            var str = MondValue.String("hello world");

            Assert.That(str.Contains("hello"), Is.True);
            Assert.That(str.Contains("asdf"), Is.False);

            var obj = MondValue.Object(new MondState());
            obj["__in"] = new MondFunction((state, args) => args[1].Type == MondValueType.Number);

            Assert.That(obj.Contains(3), Is.True);
            Assert.That(obj.Contains("hello"), Is.False);

            Assert.Throws<MondRuntimeException>(() => MondValue.False.Contains(0));
        }

        [Test]
        public void Slice()
        {
            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };

            var arr = MondValue.Array();
            var str = MondValue.String("HelloWorld");

            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });

            Assert.That(str.Slice(1, 3, 1).Equals(MondValue.String("ell")), Is.True);

            Assert.That(arr.Slice().Enumerate(state).SequenceEqual(arr.Enumerate(state)), Is.True, "clone");

            Assert.That(arr.Slice(step: -1).Enumerate(state).SequenceEqual(new MondValue[] { 5, 4, 3, 2, 1 }), Is.True, "reverse");

            Assert.That(arr.Slice(1, 3).Enumerate(state).SequenceEqual(new MondValue[] { 2, 3, 4 }), Is.True, "range");
            Assert.That(arr.Slice(3, 1).Enumerate(state).SequenceEqual(new MondValue[] { 4, 3, 2 }), Is.True, "reverse range");

            Assert.That(arr.Slice(0, 0).Enumerate(state).SequenceEqual(new MondValue[] { 1 }), Is.True, "same start and end");

            Assert.That(arr.Slice(-4, -2).Enumerate(state).SequenceEqual(new MondValue[] { 2, 3, 4 }), Is.True, "negative range");
            Assert.That(arr.Slice(-2, -4).Enumerate(state).SequenceEqual(new MondValue[] { 4, 3, 2 }), Is.True, "negative range reverse");

            Assert.That(arr.Slice(step: 2).Enumerate(state).SequenceEqual(new MondValue[] { 1, 3, 5 }), Is.True, "skip");
            Assert.That(arr.Slice(step: -2).Enumerate(state).SequenceEqual(new MondValue[] { 5, 3, 1 }), Is.True, "skip negative");

            Assert.Throws<MondRuntimeException>(() => arr.Slice(-6, 0, "out of bounds 1"));
            Assert.Throws<MondRuntimeException>(() => arr.Slice(0, 5, "out of bounds 2"));

            Assert.Throws<MondRuntimeException>(() => arr.Slice(step: 0), "invalid step");

            Assert.Throws<MondRuntimeException>(() => arr.Slice(4, 0, 1), "invalid range");
            Assert.Throws<MondRuntimeException>(() => arr.Slice(0, 4, -1), "invalid range negative");

            Assert.Throws<MondRuntimeException>(() => MondValue.Undefined.Slice(), "slice non-array");

            var empty = MondValue.Array();
            Assert.That(!empty.Slice().Enumerate(state).Any(), Is.True, "clone empty");
        }

        [Test]
        public void IndexerSetDoesNotModifyPrototype()
        {
            var prototype = MondValue.Object();
            prototype["test"] = 123;

            var obj = MondValue.Object();
            obj.Prototype = prototype;
            
            Assert.That(obj["test"], Is.EqualTo((MondValue)123));

            obj["test"] = 456;
            Assert.That(obj["test"], Is.EqualTo((MondValue)456));
            Assert.That(prototype["test"], Is.EqualTo((MondValue)123));
        }
    }
}

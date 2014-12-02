﻿using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ArrayTests
    {
        [Test]
        public void Creation()
        {
            var empty = Script.Run(@"
                return [];
            ");

            Assert.AreEqual(empty.Type, MondValueType.Array);
            Assert.True(empty.ArrayValue.SequenceEqual(Enumerable.Empty<MondValue>()));

            var array = Script.Run(@"
                var a = 'test';
                return [ 1, a, 3, 4 ];
            ");

            var expected = new MondValue[]
            {
                1, "test", 3, 4
            };

            Assert.AreEqual(array.Type, MondValueType.Array);
            Assert.True(array.ArrayValue.SequenceEqual(expected));
        }

        [Test]
        public void Indexing()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array[1];
            ");

            Assert.True(result == 2);

            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array[3];
            "));
        }

        [Test]
        public void Length()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array.length();
            ");

            Assert.True(result == 3);
        }

        [Test]
        public void Add()
        {
            var array = Script.Run(@"
                var array = [ 1, 2, 3 ];
                array.add(4);

                return array;
            ");

            var expected = new MondValue[]
            {
                1, 2, 3, 4
            };

            Assert.AreEqual(array.Type, MondValueType.Array);
            Assert.True(array.ArrayValue.SequenceEqual(expected));
        }

        [Test]
        public void Enumerator()
        {
            MondState state;
            var array = Script.Run(out state, "return [ 1, 2, 3, 4, 5 ];");

            var expected = new MondValue[]
            {
                1, 2, 3, 4, 5
            };

            Assert.AreEqual(array.Type, MondValueType.Array);
            Assert.True(array.IsEnumerable);
            Assert.True(array.Enumerate(state).SequenceEqual(expected));
        }

        [Test]
        public void Slice()
        {
            var arr = new MondValue(MondValueType.Array);
            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });

            var state = new MondState();
            state["arr"] = arr;

            Assert.True(state.Run("return global.arr[:];").Enumerate(state).SequenceEqual(new MondValue[] { 1, 2, 3, 4, 5 }), "no values");

            Assert.True(state.Run("return global.arr[3:];").Enumerate(state).SequenceEqual(new MondValue[] { 4, 5 }), "just start");

            Assert.True(state.Run("return global.arr[:2];").Enumerate(state).SequenceEqual(new MondValue[] { 1, 2, 3 }), "just end");

            Assert.True(state.Run("return global.arr[::-1];").Enumerate(state).SequenceEqual(new MondValue[] { 5, 4, 3, 2, 1 }), "just step");

            Assert.True(state.Run("return global.arr[1:2];").Enumerate(state).SequenceEqual(new MondValue[] { 2, 3 }));

            Assert.True(state.Run("return global.arr[0:4:2];").Enumerate(state).SequenceEqual(new MondValue[] { 1, 3, 5 }));

            Assert.Throws<MondCompilerException>(() => state.Run("return global.arr[::];"), "no values, extra colon");

            Assert.Throws<MondCompilerException>(() => state.Run("return global.arr[0:1:];"), "indices, extra colon");
        }
    }
}

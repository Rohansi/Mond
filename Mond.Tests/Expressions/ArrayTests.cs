using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ArrayTests
    {
        private readonly MondState _sliceState;

        public ArrayTests()
        {
            var arr = MondValue.Array();
            arr.ArrayValue.AddRange(new MondValue[] { 1, 2, 3, 4, 5 });
            _sliceState = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
                ["arr"] = arr
            };
        }

        [Test]
        public void CreateEmptyArray()
        {
            var empty = Script.Run(@"
                return [];
            ");

            Assert.AreEqual(empty.Type, MondValueType.Array);
            CollectionAssert.AreEqual(new MondValue[0], empty.AsList);
        }

        [Test]
        public void CreateWithLiterals()
        {
            var array = Script.Run(@"
                return [ 1, 2, 3, ];
            ");

            var expected = new MondValue[] { 1, 2, 3 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void CreateWithVariable()
        {
            var array = Script.Run(@"
                var a = 'test';
                return [ 1, a, 3, 4 ];
            ");

            var expected = new MondValue[] { 1, "test", 3, 4 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void Indexing()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array[1];
            ");

            Assert.AreEqual((MondValue)2, result);
        }

        [Test]
        public void IndexingOutOfBounds()
        {
            Assert.Throws<MondRuntimeException>(() => Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array[3];
            "));
        }

        [Test]
        public void IndexLoadNegative()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array[-2];
            ");

            Assert.AreEqual((MondValue)2, result);
        }

        [Test]
        public void IndexStoreNegative()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                array[-2] = 5;
                return array[1];
            ");

            Assert.AreEqual((MondValue)5, result);
        }

        [Test]
        public void IndexLoadWithObject()
        {
            var result = Script.Run(@"
                var number = { __number: () -> 1 };
                var array = [ 1, 2, 3 ];
                return array[number];
            ");

            Assert.AreEqual((MondValue)2, result);
        }

        [Test]
        public void IndexStoreWithObject()
        {
            var result = Script.Run(@"
                var number = { __number: () -> 1 };
                var array = [ 1, 2, 3 ];
                array[number] = 5;
                return array[1];
            ");

            Assert.AreEqual((MondValue)5, result);
        }

        [Test]
        [TestCase("i", 1)]
        [TestCase("j", 1)]
        [TestCase("x", 9)]
        public void IndexerLoadStore(string index, int expected)
        {
            var result = Script.Run(@"
                var i = 0, j = 0, a = [3];
                fun zero() { i++; return 0; }
                fun get() { j++; return a; }
                get()[zero()] += 6;
                return { i, j, x: a[0] };
            ");
            
            Assert.AreEqual((MondValue)expected, result[index]);
        }

        [Test]
        public void Length()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3 ];
                return array.length();
            ");

            Assert.AreEqual((MondValue)3, result);
        }

        [Test]
        public void Add()
        {
            var array = Script.Run(@"
                var array = [ 1, 2, 3 ];
                array.add(4);

                return array;
            ");

            var expected = new MondValue[] { 1, 2, 3, 4 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void SortAll()
        {
            var array = Script.Run(@"
                var array = [ 5, 0, 2, 4, 3, 1 ];
                array.sort();
                return array;
            ");

            var expected = new MondValue[] { 0, 1, 2, 3, 4, 5 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void SortRange()
        {
            var array = Script.Run(@"
                var array = [ 5, 0, 2, 4, 3, 1 ];
                array.sort(1, 4);
                return array;
            ");

            var expected = new MondValue[] { 5, 0, 2, 3, 4, 1 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void SortDescendingAll()
        {
            var array = Script.Run(@"
                var array = [ 5, 0, 2, 4, 3, 1 ];
                array.sortDescending();
                return array;
            ");

            var expected = new MondValue[] { 5, 4, 3, 2, 1, 0 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void SortDescendingRange()
        {
            var array = Script.Run(@"
                var array = [ 5, 0, 2, 4, 3, 1 ];
                array.sortDescending(1, 4);
                return array;
            ");

            var expected = new MondValue[] { 5, 4, 3, 2, 0, 1 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            CollectionAssert.AreEqual(expected, array.AsList);
        }

        [Test]
        public void Enumerator()
        {
            var array = Script.Run(out var state, @"
                return [ 1, 2, 3, 4, 5 ];
            ");

            var expected = new MondValue[] { 1, 2, 3, 4, 5 };

            Assert.AreEqual(array.Type, MondValueType.Array);
            Assert.AreEqual(true, array.IsEnumerable);
            CollectionAssert.AreEqual(expected, array.Enumerate(state));
        }

        [Test]
        public void SliceNoValues()
        {
            var expected = new MondValue[] { 1, 2, 3, 4, 5 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[:];").AsList);
        }

        [Test]
        public void SliceOnlyBegin()
        {
            var expected = new MondValue[] { 4, 5 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[3:];").AsList);
        }

        [Test]
        public void SliceOnlyEnd()
        {
            var expected = new MondValue[] { 1, 2, 3 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[:2];").AsList);
        }

        [Test]
        public void SliceOnlyStep()
        {
            var expected = new MondValue[] { 5, 4, 3, 2, 1 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[::-1];").AsList);
        }

        [Test]
        public void SliceRange()
        {
            var expected = new MondValue[] { 2, 3 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[1:2];").AsList);
        }

        [Test]
        public void SliceAllValues()
        {
            var expected = new MondValue[] { 1, 3, 5 };
            CollectionAssert.AreEqual(expected, _sliceState.Run("return global.arr[0:4:2];").AsList);
        }

        [Test]
        public void SliceNoValuesTrailingColon()
        {
            Assert.Throws<MondCompilerException>(() => _sliceState.Run("return global.arr[::];"));
        }

        [Test]
        public void SliceSomeValuesTrailingColon()
        {
            Assert.Throws<MondCompilerException>(() => _sliceState.Run("return global.arr[0:1:];"));
        }
    }
}

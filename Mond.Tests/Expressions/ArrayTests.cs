using System.Linq;
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
    }
}

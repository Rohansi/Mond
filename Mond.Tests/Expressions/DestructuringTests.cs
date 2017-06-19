using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class DestructuringTests
    {
        [Test]
        public void Array()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];      
                var [ a, b ] = array;
                return [ a, b ];     
            ");

            var expected = new MondValue[] { 1, 2 };
            CollectionAssert.AreEqual(expected, result.Array);
        }

        [Test]
        public void ArrayNotEnough()
        {
            var result = Script.Run(@"
                var array = [ 1, 2 ];      
                var [ a, b, c, d ] = array;
                return [ a, b, c, d ];     
            ");

            var expected = new[] { 1, 2, MondValue.Undefined, MondValue.Undefined };
            CollectionAssert.AreEqual(expected, result.Array);
        }

        [Test]
        public void ArrayEllipsisFirst()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];
                var [ ...start, _1 ] = array;
                return start;
            ");

            var expected = new MondValue[] { 1, 2, 3, 4 };
            CollectionAssert.AreEqual(expected, result.Array);
        }

        [Test]
        public void ArrayEllipsisMiddle()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];
                var [ _2, ...middle, _3 ] = array;
                return middle;
            ");

            var expected = new MondValue[] { 2, 3, 4 };
            CollectionAssert.AreEqual(expected, result.Array);
        }

        [Test]
        public void ArrayEllipsisLast()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];
                var [ _4, ...end ] = array;
                return end;
            ");

            var expected = new MondValue[] { 2, 3, 4, 5 };
            CollectionAssert.AreEqual(expected, result.Array);
        }

        [Test]
        public void ArrayEllipsisEmpty()
        {
            var result = Script.Run(@"
                var array = [ 1, 2 ];
                var [ x, ...y, z ] = array;
                return [ x, y, z ];
            ");

            var expected = new MondValue[0];

            Assert.AreEqual((MondValue)1, result[0]);
            CollectionAssert.AreEqual(expected, result[1].Array);
            Assert.AreEqual((MondValue)2, result[2]);
        }

        [Test]
        public void ArrayEllipsisNotEnough()
        {
            var result = Script.Run(@"
                var array = [ 1, 2 ];
                var [ a, b, c, ...d, e ] = array;
                return [ a, b, c, d, e ];
            ");
            
            Assert.AreEqual((MondValue)1, result[0], "a");
            Assert.AreEqual((MondValue)2, result[1], "b");
            Assert.AreEqual(MondValue.Undefined, result[2], "c");
            CollectionAssert.AreEqual(new MondValue[0], result[3].Array, "d");
            Assert.AreEqual(MondValue.Undefined, result[4], "e");
        }

        [Test]
        public void ArrayMultipleEllipsis()
        {
            const string multipleEllipsis = @"
                var array = [ 1, 2, 3, 4, 5 ];
                var [ ...head, middle, ...tail ] = array;
            ";

            Assert.Throws<MondCompilerException>(() => Script.Run(multipleEllipsis));
        }

        [Test]
        public void ObjectDestructuring()
        {
            var result = Script.Run(@"
                var object = {
                    foo: 1,
                    bar: 2,
                    baz: 3,
                };

                var { bar: two, baz: three } = object;
                var keys = [], values = [];
                
                foreach (var { key, value } in object)
                {
                    keys.add(key);
                    values.add(value);
                }

                return {
                    keys: keys,
                    values: values,
                    five: two + three,
                };
            ");

            var expectedKeys = new MondValue[] { "foo", "bar", "baz" };
            var expectedValues = new MondValue[] { 1, 2, 3 };
            
            CollectionAssert.AreEqual(expectedKeys, result["keys"].Array, "keys");
            CollectionAssert.AreEqual(expectedValues, result["values"].Array, "values");
            Assert.AreEqual((MondValue)5, result["five"]);
        }

        [Test]
        public void ObjectEllipsis()
        {
            const string objectEllipsis = @"
                var object = {
                    foo: 1,
                    bar: 2,
                    baz: 3,
                };

                var { foo, ...rest } = object;
            ";

            Assert.Throws<MondCompilerException>(() => Script.Run(objectEllipsis));
        }

        [Test]
        public void ObjectMissing()
        {
            var result = Script.Run(@"
                var object = {
                    foo: 'foo',
                    bar: 'bar',
                };

                var { foo, baz } = object;
                return [ foo, baz ];
            ");

            var expected = new MondValue[] { "foo", MondValue.Undefined };
            CollectionAssert.AreEqual(expected, result.Array);
        }
    }
}

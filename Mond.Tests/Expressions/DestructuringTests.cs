using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class DestructuringTests
    {
        [Test]
        public void BasicArrayDestructuring()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];      
                var [ a, b ] = array;
                var [ y, x ] = array[4:0];

                return [ a, b, x, y ];     
            ");

            var expected = new MondValue[] { 1, 2, 4, 5 };
            Assert.True(result.Array.SequenceEqual(expected));
        }

        [Test]
        public void ArrayEllipsisDestructuring()
        {
            var result = Script.Run(@"
                var array = [ 1, 2, 3, 4, 5 ];
                var [ ...start, _1 ] = array;
                var [ _2, ...middle, _3 ] = array;
                var [ _4, ...end ] = array;

                return [ start, middle, end ];
            ");

            var expectStart = new MondValue[] { 1, 2, 3, 4 };
            var expectMiddle = new MondValue[] { 2, 3, 4 };
            var expectEnd = new MondValue[] { 2, 3, 4, 5 };

            Assert.True(result[0].Array.SequenceEqual(expectStart));
            Assert.True(result[1].Array.SequenceEqual(expectMiddle));
            Assert.True(result[2].Array.SequenceEqual(expectEnd));

            var multipleEllipsis = @"
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
            var objectEllipsis = @"
                var object = {
                    foo: 1,
                    bar: 2,
                    baz: 3,
                };

                var { foo, ...rest } = object;
            ";

            Assert.True(result["keys"].Array.SequenceEqual(expectedKeys));
            Assert.True(result["values"].Array.SequenceEqual(expectedValues));
            Assert.AreEqual(5, (int)result["five"]);
            Assert.Throws<MondCompilerException>(() => Script.Run(objectEllipsis));
        }

        [Test]
        public void MissingDestructuredValue()
        {
            var undefinedArray = @"
                var array = [ 1, 2 ];
                var [ x, y, z ] = array;

                return x + z;
            ";

            var undefinedObject = Script.Run(@"
                var object = {
                    foo: 'foo',
                    bar: 'bar',
                };

                var { foo, bar, baz: x } = object;
                x = x || 5;

                return foo + x;
            ");

            Assert.Throws<MondRuntimeException>(() => Script.Run(undefinedArray));
            Assert.AreEqual(undefinedObject.ToString(), "foo5");
        }
    }
}

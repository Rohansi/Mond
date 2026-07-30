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
            Assert.That(result.AsList, Is.EqualTo(expected));
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
            Assert.That(result.AsList, Is.EqualTo(expected));
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
            Assert.That(result.AsList, Is.EqualTo(expected));
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
            Assert.That(result.AsList, Is.EqualTo(expected));
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
            Assert.That(result.AsList, Is.EqualTo(expected));
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

            Assert.That(result[0], Is.EqualTo((MondValue)1));
            Assert.That(result[1].AsList, Is.EqualTo(expected));
            Assert.That(result[2], Is.EqualTo((MondValue)2));
        }

        [Test]
        public void ArrayEllipsisNotEnough()
        {
            var result = Script.Run(@"
                var array = [ 1, 2 ];
                var [ a, b, c, ...d, e ] = array;
                return [ a, b, c, d, e ];
            ");
            
            Assert.That(result[0], Is.EqualTo((MondValue)1), "a");
            Assert.That(result[1], Is.EqualTo((MondValue)2), "b");
            Assert.That(result[2], Is.EqualTo(MondValue.Undefined), "c");
            Assert.That(result[3].AsList, Is.EqualTo(new MondValue[0]), "d");
            Assert.That(result[4], Is.EqualTo(MondValue.Undefined), "e");
        }

        [Test]
        public void ArrayEllipsisNotEnoughTailOnly()
        {
            var result = Script.Run(@"
                var [ ...x, y, z ] = [ 1, 2 ];
                return [ x, y, z ];
            ");

            Assert.That(result[0].AsList, Is.EqualTo(new MondValue[0]));
            Assert.That(result.AsList[1], Is.EqualTo((MondValue)1));
            Assert.That(result.AsList[2], Is.EqualTo((MondValue)2));
        }

        [Test]
        public void ArrayEllipsisTailOnly()
        {
            var result = Script.Run(@"
                var [ ...x, y, z ] = [ 1, 2, 3 ];
                return [ x, y, z ];
            ");

            Assert.That(result[0].AsList, Is.EqualTo(new MondValue[] { 1 }));
            Assert.That(result.AsList[1], Is.EqualTo((MondValue)2));
            Assert.That(result.AsList[2], Is.EqualTo((MondValue)3));
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
            
            Assert.That(result["keys"].AsList, Is.EqualTo(expectedKeys), "keys");
            Assert.That(result["values"].AsList, Is.EqualTo(expectedValues), "values");
            Assert.That(result["five"], Is.EqualTo((MondValue)5));
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
            Assert.That(result.AsList, Is.EqualTo(expected));
        }
    }
}

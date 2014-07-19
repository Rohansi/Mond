using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class SequenceTests
    {
        [Test]
        public void FizzBuzz()
        {
            var result = Script.Run(@"
                seq fizzBuzz() {
                    var n = 1;

                    while (true) {
                        var str = '';

                        if (n % 3 == 0)
                            str += 'Fizz';
                        
                        if (n % 5 == 0)
                            str += 'Buzz';

                        if (str == '')
                            str += n;

                        yield str;
                        n++;
                    }
                };
                
                var values = [];

                foreach (var str in fizzBuzz()) {
                    values.add(str);

                    if (values.length() >= 15)
                        break;
                }

                return values;
            ");

            var expected = new MondValue[]
            {
                "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz"
            };

            Assert.AreEqual(result.Type, MondValueType.Array);
            Assert.True(result.ArrayValue.SequenceEqual(expected));
        }

        [Test]
        public void NestedSequence()
        {
            var result = Script.Run(@"
                seq expand(pairs) {
                    seq repeat(value, count) {
                        for (var i = 0; i < count; i++)
                            yield value;
                    }

                    foreach (var pair in pairs)
                        foreach (var v in repeat(pair.v, pair.n))
                            yield v;
                }

                var input = [{v: 1, n: 2}, {v: 'hi', n: 5}];
                var output = expand(input);

                var array = [];

                foreach (var v in output)
                    array.add(v);

                return array;
            ");

            var expected = new MondValue[]
            {
                1, 1, "hi", "hi", "hi", "hi", "hi"
            };

            Assert.AreEqual(result.Type, MondValueType.Array);
            Assert.True(result.ArrayValue.SequenceEqual(expected));
        }

        [Test]
        public void NestedForeach()
        {
            
        }

        [Test]
        public void Comprehension()
        {

        }
    }
}

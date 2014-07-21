using System;
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
            MondState state;
            var result = Script.Run(out state, @"
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
                }

                return fizzBuzz();
            ");

            var expected = new MondValue[]
            {
                "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz"
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).Take(expected.Length).SequenceEqual(expected));
        }

        [Test]
        public void NestedSequence()
        {
            MondState state;
            var result = Script.Run(out state, @"
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
                return expand(input);
            ");

            var expected = new MondValue[]
            {
                1, 1, "hi", "hi", "hi", "hi", "hi"
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).SequenceEqual(expected));
        }

        [Test]
        public void Comprehension()
        {
            MondState state;
            var result = Script.Run(out state, @"
                fun double(x) -> x * 2;
                fun half(x) -> x / 2;

                fun isNumber(x) -> x.getType() == 'number';
                fun above10(x) -> x > 10;

                var input = [ [5, null, 15, 20], [1, 100, 1000, 'test'] ];
            
                return [double(x) + half(x) : list in input, x in list, isNumber(x), above10(x)];
            ");

            var expected = new MondValue[]
            {
                37.5, 50, 250, 2500
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).SequenceEqual(expected));
        }
    }
}

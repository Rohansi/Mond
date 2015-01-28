using System.Linq;
using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class SequenceTests
    {
        [Test]
        public void Sequence()
        {
            MondState state;
            var result = Script.Run(out state, @"
                var test = seq () {
                    for (var i = 1; i <= 10; i++) {
                        if (i > 5)
                            yield break;
                        
                        yield i;
                    }
                };

                return test();
            ");
            
            var expected = new MondValue[]
            {
                1, 2, 3, 4, 5
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).Take(expected.Length).SequenceEqual(expected));
        }

        [Test]
        public void SequenceScope()
        {
            MondState state;
            var result = Script.Run(out state, @"
                seq scope() {
                    {
                        var a = 10;
                        yield a;
                    }

                    {
                        var a;
                        yield a;
                    }
                }

                return scope();
            ");

            var expected = new[]
            {
                10, MondValue.Undefined
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).SequenceEqual(expected));
        }

        [Test]
        public void SequenceErrors()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                seq test() {
                    return;
                }
            "), "can't use return in seq");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() {
                    yield 1;
                }
            "), "can't use yield in fun");

            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() {
                    yield break;
                }
            "), "can't use yield in fun");
        }

        [Test]
        public void FizzBuzz()
        {
            MondState state;
            var result = Script.Run(out state, @"
                seq fizzBuzz() {
                    var n = 1;

                    while (true) {
                        if (n % 15 == 0)
                            yield 'FizzBuzz';
                        else if (n % 3 == 0)
                            yield 'Fizz';
                        else if (n % 5 == 0)
                            yield 'Buzz';
                        else
                            yield '' + n;

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

        [Test]
        public void VariableLengthArguments()
        {
            MondState state;
            var result = Script.Run(out state, @"
                seq values(...args) {
                    foreach (var x in args)
                        yield x;
                }

                return values(1, 2, 3);
            ");

            var expected = new MondValue[]
            {
                1, 2, 3
            };

            Assert.True(result.IsEnumerable);
            Assert.True(result.Enumerate(state).SequenceEqual(expected));
        }

        [Test]
        public void LambdaInLoop()
        {
            MondState state;
            var result = Script.Run(out state, @"
                seq ints() {
                    var i = 0;
                    while (true) {
                        var ii = i++;
                        yield () -> ii;
                    }
                }

                return ints();
            ");

            Assert.True(result.IsEnumerable);

            result = result.Enumerate(state).Skip(4).FirstOrDefault();
            Assert.True(state.Call(result) == 4);
        }

        [Test]
        public void YieldExpression()
        {
            var result = Script.Run(@"
                var result = 0;

                seq adder() {
                    result = (yield) + (yield);
                }

                var e = adder().getEnumerator();
                e.moveNext();

                e.moveNext(10);
                e.moveNext(5);

                return result;
            ");

            Assert.True(result == 15);
        }

        [Test]
        public void InterlacedYieldExpression()
        {
            var result = Script.Run(@"
                seq adder() {
                    yield (yield) + (yield);
                }

                var ae = adder().getEnumerator();
                var be = adder().getEnumerator();

                ae.moveNext();
                be.moveNext();

                ae.moveNext(1);
                be.moveNext(10);

                ae.moveNext(5);
                be.moveNext(15);

                return [ ae.current, be.current ];
            ");

            Assert.True(result[0] == 6);
            Assert.True(result[1] == 25);
        }
    }
}

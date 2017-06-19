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
            var result = Script.Run(out var state, @"
                var test = seq () {
                    for (var i = 1; i <= 10; i++) {
                        if (i > 5)
                            return;
                        
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
            CollectionAssert.AreEqual(expected, result.Enumerate(state));
        }

        [Test]
        public void SequenceExpression()
        {
            var result = Script.Run(out var state, @"
                return (seq () -> 10)();
            ");

            Assert.True(result.IsEnumerable);

            var enumerator = state.Call(result["getEnumerator"]);
            Assert.AreEqual(MondValue.False, state.Call(enumerator["moveNext"]));
            Assert.AreEqual((MondValue)10, enumerator["current"]);
        }

        [Test]
        public void SequenceScope()
        {
            var result = Script.Run(out var state, @"
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
            CollectionAssert.AreEqual(expected, result.Enumerate(state));
        }

        [Test]
        public void YieldReturnInFunction()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() {
                    yield 1;
                }
            "));
        }

        [Test]
        public void YieldBreakInFunction()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                fun test() {
                    yield break;
                }
            "));
        }

        [Test]
        public void SequenceReturn()
        {
            var result = Script.Run(@"
                seq test() {
                    yield 1;
                    return 2;
                }

                var enumerator = test().getEnumerator();

                fun check(result, current) {
                    if (enumerator.moveNext() != result)
                        return false;

                    return enumerator.current == current;
                }

                return check(true, 1) &&
                       check(false, 2) &&
                       check(false, 2);
            ");

            Assert.AreEqual(MondValue.True, result);
        }
        
        [Test]
        public void FizzBuzz()
        {
            var result = Script.Run(out var state, @"
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
            CollectionAssert.AreEqual(expected, result.Enumerate(state).Take(expected.Length));
        }

        [Test]
        public void NestedSequence()
        {
            var result = Script.Run(out var state, @"
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
            CollectionAssert.AreEqual(expected, result.Enumerate(state));
        }

        [Test]
        public void VariableLengthArguments()
        {
            var result = Script.Run(out var state, @"
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
            CollectionAssert.AreEqual(expected, result.Enumerate(state));
        }

        [Test]
        public void LambdaInLoop()
        {
            var result = Script.Run(out var state, @"
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
            Assert.AreEqual((MondValue)4, state.Call(result));
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

            Assert.AreEqual((MondValue)15, result);
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

            Assert.AreEqual((MondValue)6, result[0]);
            Assert.AreEqual((MondValue)25, result[1]);
        }
    }
}

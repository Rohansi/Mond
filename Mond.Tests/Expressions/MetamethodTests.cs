using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class MetamethodTests
    {
        [Test]
        public void Indexer()
        {
            // __get, __set

            var result = Script.Run(@"
                var target = {};
                var handler = {
                    get: fun (this, index) {
                        if (this == target && index.getType() == 'string')
                            return target[index];
                        return undefined;
                    },
                    set: fun (this, index, value) {
                        if (this == target && index.getType() == 'string')
                            target[index] = value;
                    }
                };

                return proxyCreate(target, handler);
            ");

            result["test"] = 123;
            Assert.That(result["test"], Is.EqualTo((MondValue)123));

            result[123] = "test";
            Assert.That(result[123], Is.Not.EqualTo((MondValue)"test"));
        }

        [Test]
        public void Call()
        {
            // __call

            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };
            var result = state.Run(@"
                var obj = {
                    __call: fun (this, x, y) {
                        return x + y;
                    }
                };

                global.obj = obj;

                return obj(1, 3) + obj(...[1, 3]);
            ");

            Assert.That(result, Is.EqualTo((MondValue)8));

            Assert.That(state.Call(state["obj"], 1, 3), Is.EqualTo((MondValue)4));
        }

        [Test]
        public void ImplicitNumberCast()
        {
            // __number

            var result = Script.Run(@"
                var obj = {
                    __number: fun (this) -> 4
                };

                return (1 + obj) + (obj + 1);
            ");

            Assert.That(result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void ImplicitBoolCast()
        {
            // __bool

            var result = Script.Run(@"
                var obj = {
                    __bool: fun (this) -> false
                };

                return obj ? 'yes' : 'no';
            ");

            Assert.That(result, Is.EqualTo((MondValue)"no"));
        }

        [Test]
        public void ImplicitStringCast()
        {
            // __string

            var result = Script.Run(@"
                var obj = {
                    __string: fun (this) -> 'hello'
                };

                return ('' + obj) + (obj + '') + (obj.toString());
            ");

            Assert.That(result, Is.EqualTo((MondValue)"hellohellohello"));
        }

        [Test]
        public void Serialize()
        {
            // __serialize

            var result = Script.Run(@"
                var obj = {
                    __serialize: fun (this) -> 'serialized'
                };
                
                return obj.serialize();
            ");

            Assert.That(result == "\"serialized\"", Is.True);
        }

        [Test]
        public void Relational()
        {
            // __eq, __gt, __gte, __lt, __lte

            var result = Script.Run(@"
                var obj = {
                    __eq: fun (this, value) {
                        return 4 == value;
                    },
                    __neq: fun (this, value) {
                        return 4 != value;
                    },
                    __gt: fun (this, value) {
                        return 4 > value;
                    },
                    __gte: fun (this, value) {
                        return 3 >= value;
                    },
                    __lt: fun (this, value) {
                        return value < 6;
                    },
                    __lte: fun (this, value ) {
                        return value <= 5;
                    }
                };

                return obj;
            ");

            Assert.That(result == 4, Is.True, "==");

            Assert.That(result != 3, Is.True, "!=");

            Assert.That(result > 3, Is.True, ">");

            Assert.That(result >= 3, Is.True, ">=");

            Assert.That(result < 5, Is.True, "<");

            Assert.That(result <= 5, Is.True, "<=");
        }

        [Test]
        public void In()
        {
            // __in

            var result = Script.Run(@"
                var obj = {
                    __in: fun (this, value) {
                        return value == 4;
                    }
                };

                return obj;
            ");

            Assert.That(result.Contains(4), Is.True);

            Assert.That(result.Contains(5), Is.False);
        }

        [Test]
        public void Math()
        {
            // __add, __sub, __mul, __div, __mod, __pow, __neg

            var result = Script.Run(@"
                var obj = {
                    __add: fun (this, value) -> value,
                    __sub: fun (this, value) -> value,
                    __mul: fun (this, value) -> value,
                    __div: fun (this, value) -> value,
                    __mod: fun (this, value) -> value,
                    __pow: fun (this, value) -> value,
                    __neg: fun (this) -> 100
                };

                return obj;
            ");

            Assert.That(result + 1 == 1, Is.True, "+");

            Assert.That(result - 2 == 2, Is.True, "-");

            Assert.That(result * 3 == 3, Is.True, "*");

            Assert.That(result / 4 == 4, Is.True, "/");

            Assert.That(result % 5 == 5, Is.True, "%");

            Assert.That(result.Pow(6) == 6, Is.True, "**");

            Assert.That(-result == 100, Is.True, "neg");
        }

        [Test]
        public void BinaryMath()
        {
            // __and, __or, __xor, __lshift, __rshift, __not

            var result = Script.Run(@"
                var obj = {
                    __and: fun (this, value) -> value,
                    __or: fun (this, value) -> value,
                    __xor: fun (this, value) -> value,
                    __lshift: fun (this, value) -> value,
                    __rshift: fun (this, value) -> value,
                    __not: fun (this) -> 100,
                };

                return obj;
            ");

            Assert.That((result & 1) == 1, Is.True, "&");

            Assert.That((result | 2) == 2, Is.True, "|");

            Assert.That((result ^ 3) == 3, Is.True, "^");

            Assert.That((result << 4) == 4, Is.True, "<<");

            Assert.That((result >> 5) == 5, Is.True, ">>");

            Assert.That(result.LShift(6) == 6, Is.True, "LShift");

            Assert.That(result.RShift(7) == 7, Is.True, "RShift");

            Assert.That(~result == 100, Is.True, "~");
        }

        [Test]
        public void Slice()
        {
            var result = Script.Run(out var state, @"
                var obj = {
                    __slice: fun (this, start, end, step) -> [ start, end, step ]
                };

                return obj[1:2:3];
            ");

            var expected = new MondValue[]
            {
                1, 2, 3
            };

            Assert.That(result.Type, Is.EqualTo(MondValueType.Array));
            Assert.That(result.Enumerate(state), Is.EqualTo(expected));
        }

        [Test]
        public void Hash()
        {
            var result = Script.Run(@"
                fun new() {
                    return {
                        __eq: fun() -> true,
                        __hash: fun() -> 123
                    };
                }

                var obj = {};
                var key1 = new();
                var key2 = new();

                obj[key1] = 456;
                return obj[key2];
            ");

            Assert.That(result, Is.EqualTo((MondValue)456));
        }
    }
}

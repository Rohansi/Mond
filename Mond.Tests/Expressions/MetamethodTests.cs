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
            Assert.AreEqual((MondValue)123, result["test"]);

            result[123] = "test";
            Assert.AreNotEqual((MondValue)"test", result[123]);
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

            Assert.AreEqual((MondValue)8, result);

            Assert.AreEqual((MondValue)4, state.Call(state["obj"], 1, 3));
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

            Assert.AreEqual((MondValue)10, result);
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

            Assert.AreEqual((MondValue)"no", result);
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

            Assert.AreEqual((MondValue)"hellohellohello", result);
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

            Assert.True(result == "\"serialized\"");
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

            Assert.True(result == 4, "==");

            Assert.True(result != 3, "!=");

            Assert.True(result > 3, ">");

            Assert.True(result >= 3, ">=");

            Assert.True(result < 5, "<");

            Assert.True(result <= 5, "<=");
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

            Assert.True(result.Contains(4));

            Assert.False(result.Contains(5));
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

            Assert.True(result + 1 == 1, "+");

            Assert.True(result - 2 == 2, "-");

            Assert.True(result * 3 == 3, "*");

            Assert.True(result / 4 == 4, "/");

            Assert.True(result % 5 == 5, "%");

            Assert.True(result.Pow(6) == 6, "**");

            Assert.True(-result == 100, "neg");
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

            Assert.True((result & 1) == 1, "&");

            Assert.True((result | 2) == 2, "|");

            Assert.True((result ^ 3) == 3, "^");

            Assert.True((result << 4) == 4, "<<");

            Assert.True((result >> 5) == 5, ">>");

            Assert.True(result.LShift(6) == 6, "LShift");

            Assert.True(result.RShift(7) == 7, "RShift");

            Assert.True(~result == 100, "~");
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

            Assert.AreEqual(MondValueType.Array, result.Type);
            CollectionAssert.AreEqual(expected, result.Enumerate(state));
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

            Assert.AreEqual((MondValue)456, result);
        }
    }
}

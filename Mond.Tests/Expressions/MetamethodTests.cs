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
                var inner = {};
                var obj = {
                    __get: fun (this, index) {
                        if (index.getType() == 'string')
                            return inner[index];
                        return undefined;
                    },
                    __set: fun (this, index, value) {
                        if (index.getType() == 'string')
                            inner[index] = value;
                    }
                };

                return obj;
            ");

            result["test"] = 123;
            Assert.True(result["test"] == 123);

            result[123] = "test";
            Assert.False(result[123] == "test");
        }

        [Test]
        public void Call()
        {
            // __call

            var state = new MondState();
            var result = state.Run(@"
                var obj = {
                    __call: fun (this, x, y) {
                        return x + y;
                    }
                };

                global.obj = obj;

                return obj(1, 3) + obj(...[1, 3]);
            ");

            Assert.True(result == 8);

            Assert.True(state.Call(state["obj"], 1, 3) == 4);
        }

        [Test]
        public void ImplicitCasts()
        {
            // __number, __bool, __string

            var result = Script.Run(@"
                var obj = {
                    __number: fun (this) -> 4
                };

                return (1 + obj) + (obj + 1);
            ");

            Assert.True(result == 10);

            result = Script.Run(@"
                var obj = {
                    __bool: fun (this) -> false
                };

                return obj ? 'yes' : 'no';
            ");

            Assert.True(result == "no");

            result = Script.Run(@"
                var obj = {
                    __string: fun (this) -> 'hello'
                };

                return ('' + obj) + (obj + '') + (obj.toString());
            ");

            Assert.True(result == "hellohellohello");
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
        public void Comparison()
        {
            // __eq, __gt

            var result = Script.Run(@"
                var obj = {
                    __eq: fun (this, value) {
                        return 4 == value;
                    },
                    __gt: fun (this, value) {
                        return 4 > value;
                    }
                };

                return obj;
            ");

            Assert.True(result == 4, "==");

            Assert.True(result > 3, ">");

            Assert.True(result < 5, "<");

            Assert.True(result >= 3, ">=");

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
    }
}

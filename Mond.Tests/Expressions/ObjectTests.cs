using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ObjectTests
    {
        [Test]
        [TestCase("return { a: 123, b: 456 };")]
        [TestCase("var a = 123, b = 456; return { a, b };")]
        [TestCase("return { a: 123, b: 456, };")]
        [TestCase("var obj = {}; obj.a = 123; obj.b = 456; return obj;")]
        [TestCase("var obj = {}; obj['a'] = 123; obj['b'] = 456; return obj;")]
        [TestCase("var obj = { a: 123, b: 456 }; return { a: obj.a, b: obj.b };")]
        [TestCase("var obj = { a: 123, b: 456 }; return { a: obj['a'], b: obj['b'] };")]
        public void CreationAndIndexing(string source)
        {
            var result = Script.Run(source);

            Assert.AreEqual((MondValue)123, result["a"]);
            Assert.AreEqual((MondValue)456, result["b"]);
        }

        [Test]
        public void FieldLoadStore()
        {
            var result = Script.Run(@"
                var i = 0, o = { x: 3 };
                fun get() { i++; return o; }
                get().x += 6;
                return { i, x: o.x };
            ");

            Assert.True(result.Type == MondValueType.Object);
            Assert.True(result["i"] == 1);
            Assert.True(result["x"] == 9);
        }

        [Test]
        public void AnonymousFunctionDebugName()
        {
            var obj = Script.Run(out var state, @"
                return {
                    fun123: fun () { error('test'); },
                    seq456: seq () { error('test'); },
                };
            ");

            Assert.AreEqual(MondValueType.Function, obj["fun123"].Type);
            Assert.AreEqual(MondValueType.Function, obj["seq456"].Type);

            var funEx = Assert.Throws<MondRuntimeException>(() => state.Call(obj["fun123"]));
            StringAssert.Contains("fun123", funEx.ToString());

            var seqEx = Assert.Throws<MondRuntimeException>(() =>
            {
                var enumerator = state.Call(obj["seq456"]);
                state.Call(enumerator["moveNext"]);
            });
            StringAssert.Contains("seq456", seqEx.ToString());
        }

        [Test]
        public void Classes()
        {
            var result = Script.Run(@"
                fun Base() {
                    return {
                        number: fun (_) -> 10,
                        add: fun (_, x, y) -> x + y
                    };
                }

                fun Class() {
                    var base, this = {
                        number: fun (_) -> this.add(base.number(), 5)
                    };

                    base = Base();
                    this.setPrototype(base);

                    return this;
                }

                var a = Class();
                return a.number();
            ");

            Assert.AreEqual((MondValue)15, result);
        }
    }
}

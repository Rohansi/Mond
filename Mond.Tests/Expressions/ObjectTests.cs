using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class ObjectTests
    {
        [Test]
        public void Creation()
        {
            const string source1 = @"
                return { a: 123, b: 456 };
            ";

            const string source2 = @"
                var a = 123, b = 456;
                return { a, b };
            ";

            const string source3 = @"
                return { a: 123, b: 456, };
            ";

            var sources = new[] { source1, source2, source3 };

            foreach (var source in sources)
            {
                var result = Script.Run(source);

                Assert.True(result["a"] == 123);
                Assert.True(result["b"] == 456);
            }
        }

        [Test]
        public void Indexing()
        {
            const string source1 = @"
                var obj = {};
                obj.a = 123;
                obj.b = 456;
                return obj;
            ";

            const string source2 = @"
                var obj = {};
                obj['a'] = 123;
                obj['b'] = 456;
                return obj;
            ";

            const string source3 = @"
                var obj = { a: 123, b: 456 };
                return { a: obj.a, b: obj.b };
            ";

            const string source4 = @"
                var obj = { a: 123, b: 456 };
                return { a: obj['a'], b: obj['b'] };
            ";

            var sources = new[] { source1, source2, source3, source4 };

            foreach (var source in sources)
            {
                var result = Script.Run(source);

                Assert.True(result["a"] == 123);
                Assert.True(result["b"] == 456);
            }
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
            MondState state;
            var obj = Script.Run(out state, @"
                return {
                    fun123: fun () { error('test'); },
                    seq456: seq () { error('test'); },
                };
            ");

            Assert.True(obj["fun123"].Type == MondValueType.Function);
            Assert.True(obj["seq456"].Type == MondValueType.Function);

            var funEx = Assert.Throws<MondRuntimeException>(() => state.Call(obj["fun123"]));
            Assert.True(funEx.Message.Contains("fun123"));

            var seqEx = Assert.Throws<MondRuntimeException>(() =>
            {
                var enumerator = state.Call(obj["seq456"]);
                state.Call(enumerator["moveNext"]);
            });
            Assert.True(seqEx.Message.Contains("seq456"));
        }
    }
}

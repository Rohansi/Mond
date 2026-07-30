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

            Assert.That(result["a"], Is.EqualTo((MondValue)123));
            Assert.That(result["b"], Is.EqualTo((MondValue)456));
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

            Assert.That(result.Type == MondValueType.Object, Is.True);
            Assert.That(result["i"] == 1, Is.True);
            Assert.That(result["x"] == 9, Is.True);
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

            Assert.That(obj["fun123"].Type, Is.EqualTo(MondValueType.Function));
            Assert.That(obj["seq456"].Type, Is.EqualTo(MondValueType.Function));

            var funEx = Assert.Throws<MondRuntimeException>(() => state.Call(obj["fun123"]));
            Assert.That(funEx.ToString(), Does.Contain("fun123"));

            var seqEx = Assert.Throws<MondRuntimeException>(() =>
            {
                var enumerator = state.Call(obj["seq456"]);
                state.Call(enumerator["moveNext"]);
            });
            Assert.That(seqEx.ToString(), Does.Contain("seq456"));
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

            Assert.That(result, Is.EqualTo((MondValue)15));
        }

        [Test]
        public void Classes2()
        {
            const string script =
                """
                fun class(spec) {
                    const ctor = spec.new;
                    fun newInst(...args) {
                        const inst = {};
                        if (ctor.getType() == "function") {
                            ctor(inst, ...args);
                        }
                        return inst.setPrototype(spec);
                    };
                    spec.new = newInst;
                    spec.lock();
                    return spec;
                }

                const Person = class({
                    new(this, name) {
                        this.name = name;
                    },
                    
                    greeting(this) -> "hello " + this.name
                });

                const rohan = Person.new("Rohan");
                return rohan.greeting();
                """;
            var result = Script.Run(script);
            Assert.That(result, Is.EqualTo((MondValue)"hello Rohan"));
        }

        [Test]
        public void MethodSyntaxWithoutSpecifier()
        {
            const string script =
                """
                var obj = {
                    method(this, x, y) -> x + y,
                };
                return obj.method(1, 2);
                """;
            var result = Script.Run(script);
            Assert.That(result, Is.EqualTo((MondValue)3));
        }

        [Test]
        public void MethodSyntaxFunctionSpecifier()
        {
            const string script =
                """
                var obj = {
                    fun method(this, x, y) -> x + y,
                };
                return obj.method(1, 2);
                """;
            var result = Script.Run(script);
            Assert.That(result, Is.EqualTo((MondValue)3));
        }

        [Test]
        public void MethodSyntaxFunctionSpecifierRequiresName()
        {
            const string script =
                """
                var obj = {
                    fun (this, x, y) -> x + y,
                };
                return obj.method(1, 2);
                """;
            Assert.Throws<MondCompilerException>(() => Script.Run(script));
        }

        [Test]
        public void MethodSyntaxSequenceSpecifier()
        {
            const string script =
                """
                var obj = {
                    seq method(this, x, y) {
                        yield x;
                        yield y;
                    },
                };
                return obj.method(1, 2);
                """;
            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };
            var result = state.Run(script);
            var values = result.Enumerate(state);
            Assert.That(values, Is.EqualTo(new MondValue[] { 1, 2 }));
        }

        [Test]
        public void MethodSyntaxDecoratedFunction()
        {
            const string script =
                """
                fun offset(f) {
                    return fun (...args) -> f(...args) + 1;
                }
                
                var obj = {
                    @offset
                    fun method(this, x, y) -> x + y,
                };
                return obj.method(1, 2);
                """;
            var result = Script.Run(script);
            Assert.That(result, Is.EqualTo((MondValue)4));
        }
    }
}

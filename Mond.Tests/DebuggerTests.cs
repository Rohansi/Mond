using System;
using Mond.Debugger;
using NUnit.Framework;

namespace Mond.Tests
{
    [TestFixture]
    public class DebuggerTests
    {
        [Test]
        public void DebuggerStatement()
        {
            var callbackCalled = false;
            RunWithDebugger("debugger;", _ => callbackCalled = true, 1);
            Assert.That(callbackCalled, Is.EqualTo(true));
        }
        
        [Test]
        public void ReadLocal()
        {
            const string code =
                """
                var x = 10;
                debugger;
                """;

            RunWithDebugger(code, context =>
            {
                var locals = context.GetLocals();
                Assert.That(locals["x"], Is.EqualTo((MondValue)10), "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");
            }, 1);
        }
        
        [Test]
        public void WriteLocal()
        {
            const string code =
                """
                var x = 10;
                debugger;
                return x;
                """;

            var result = RunWithDebugger(code, context =>
            {
                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.That(result, Is.EqualTo((MondValue)11));
        }
        
        [Test]
        public void ReadArgument()
        {
            const string code =
                """
                fun test(x) {
                    debugger;
                }
                test(10);
                """;

            RunWithDebugger(code, context =>
            {
                var locals = context.GetLocals();
                Assert.That(locals["x"], Is.EqualTo((MondValue)10), "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");
            }, 1);
        }
        
        [Test]
        public void WriteArgument()
        {
            const string code =
                """
                fun test(x) {
                    debugger;
                    return x;
                }
                return test(10);
                """;

            var result = RunWithDebugger(code, context =>
            {
                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.That(result, Is.EqualTo((MondValue)11));
        }
        
        [Test]
        public void ReadUpvalue()
        {
            const string code =
                """
                var x = 10;
                fun test() {
                    debugger;
                    return x;
                }
                return test();
                """;

            RunWithDebugger(code, context =>
            {
                var locals = context.GetLocals();
                Assert.That(locals["x"], Is.EqualTo((MondValue)10), "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");
            }, 1);
        }
        
        [Test]
        public void WriteUpvalue()
        {
            const string code =
                """
                var x = 10;
                fun test() {
                    debugger;
                    return x;
                }
                return test();
                """;

            var result = RunWithDebugger(code, context =>
            {
                var xValue = context.Evaluate("x");
                Assert.That(xValue, Is.EqualTo((MondValue)10), "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.That(result, Is.EqualTo((MondValue)11));
        }

        [Test]
        public void CallstackFunction()
        {
            const string script =
                """
                fun funcA() {
                    debugger;
                }
                
                fun funcB() {
                    funcA();
                }
                
                return funcB();
                """;

            RunWithDebugger(script, context =>
            {
                Assert.That(context.CallStack.Count, Is.EqualTo(3));
                Assert.That(context.CallStack[0].Function, Is.EqualTo("funcA"));
                Assert.That(context.CallStack[1].Function, Is.EqualTo("funcB"));
                Assert.That(context.CallStack[2].Function, Is.EqualTo("<top level>"));
            }, 1);
        }

        [Test]
        public void CallstackSequence()
        {
            const string script =
                """
                seq sequence() {
                    debugger;
                }

                foreach (var x in sequence()) {
                
                }
                """;

            RunWithDebugger(script, context =>
            {
                Assert.That(context.CallStack.Count, Is.EqualTo(2));
                Assert.That(context.CallStack[0].Function, Is.EqualTo("sequence.moveNext"));
                Assert.That(context.CallStack[1].Function, Is.EqualTo("<top level>"));
            }, 1);
        }

        [Test]
        public void CallstackMixed()
        {
            const string script =
                """
                fun function() {
                    debugger;
                }
                
                seq sequence() {
                    function();
                }

                foreach (var x in sequence()) {

                }
                """;

            RunWithDebugger(script, context =>
            {
                Assert.That(context.CallStack.Count, Is.EqualTo(3));
                Assert.That(context.CallStack[0].Function, Is.EqualTo("function"));
                Assert.That(context.CallStack[1].Function, Is.EqualTo("sequence.moveNext"));
                Assert.That(context.CallStack[2].Function, Is.EqualTo("<top level>"));
            }, 1);
        }

        private static MondValue RunWithDebugger(string code, Action<MondDebugContext> debugCallback, int expectedBreakCount)
        {
            var debugger = new Debugger(debugCallback);

            var state = Script.NewState();
            state.Debugger = debugger;
            var result = state.Run(code);
            
            Assert.That(debugger.BreakCount, Is.EqualTo(expectedBreakCount));

            return result;
        }

        private class Debugger(Action<MondDebugContext> onBreak) : MondDebugger
        {
            public int BreakCount { get; private set; }

            protected override MondDebugAction OnBreak(MondDebugContext context, int address)
            {
                BreakCount++;
                onBreak(context);
                return MondDebugAction.Run;
            }
        }
    }
}

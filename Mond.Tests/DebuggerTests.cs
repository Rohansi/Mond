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
            Assert.AreEqual(true, callbackCalled);
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
                Assert.AreEqual((MondValue)10, locals["x"], "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");
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
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.AreEqual((MondValue)11, result);
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
                Assert.AreEqual((MondValue)10, locals["x"], "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");
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
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.AreEqual((MondValue)11, result);
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
                Assert.AreEqual((MondValue)10, locals["x"], "GetLocals");

                var xValue = context.Evaluate("x");
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");
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
                Assert.AreEqual((MondValue)10, xValue, "Evaluate");

                context.Evaluate("x = 11");
            }, 1);

            Assert.AreEqual((MondValue)11, result);
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
                Assert.AreEqual(3, context.CallStack.Count);
                Assert.AreEqual("funcA", context.CallStack[0].Function);
                Assert.AreEqual("funcB", context.CallStack[1].Function);
                Assert.AreEqual("<top level>", context.CallStack[2].Function);
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
                Assert.AreEqual(2, context.CallStack.Count);
                Assert.AreEqual("sequence.moveNext", context.CallStack[0].Function);
                Assert.AreEqual("<top level>", context.CallStack[1].Function);
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
                Assert.AreEqual(3, context.CallStack.Count);
                Assert.AreEqual("function", context.CallStack[0].Function);
                Assert.AreEqual("sequence.moveNext", context.CallStack[1].Function);
                Assert.AreEqual("<top level>", context.CallStack[2].Function);
            }, 1);
        }

        private static MondValue RunWithDebugger(string code, Action<MondDebugContext> debugCallback, int expectedBreakCount)
        {
            var debugger = new Debugger(debugCallback);

            var state = Script.NewState();
            state.Debugger = debugger;
            var result = state.Run(code);
            
            Assert.AreEqual(expectedBreakCount, debugger.BreakCount);

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

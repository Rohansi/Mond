using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mond.Compiler;
using Mond.VirtualMachine;

namespace Mond.Debugger
{
    public class MondDebugContext
    {
        private const string LocalObjectName = "__local";

        private readonly MondState _state;
        private readonly int _address;
        private readonly Frame _locals;
        private readonly Frame _args;

        private readonly MondValue _localObject;

        public readonly MondProgram Program;
        public readonly MondDebugInfo DebugInfo;

        public readonly ReadOnlyCollection<CallStackEntry> CallStack;

        internal MondDebugContext(
            MondState state, MondProgram program, int address,
            Frame locals, Frame args,
            ReturnAddress[] callStack,  int callStackTop, int callStackBottom)
        {
            _state = state;
            _address = address;
            _locals = locals;
            _args = args;

            Program = program;
            DebugInfo = program.DebugInfo;

            CallStack = GenerateCallStack(address, callStack, callStackTop, callStackBottom).AsReadOnly();

            _localObject = CreateLocalObject();
        }

        public MondValue Evaluate(string expression)
        {
            var options = new MondCompilerOptions();

            var lexer = new Lexer("return " + expression, "debug", options);
            var parser = new Parser(lexer);

            var expr = parser.ParseStatement(false);

            var rewriter = new DebugExpressionRewriter(LocalObjectName);
            expr = expr.Accept(rewriter).Simplify();
            expr.SetParent(null);

            var oldLocal = _state[LocalObjectName];
            _state[LocalObjectName] = _localObject;

            var program = new ExpressionCompiler(options).Compile(expr);
            var result = _state.Load(program);

            _state[LocalObjectName] = oldLocal;

            return result;
        }

        private MondValue CreateLocalObject()
        {
            var obj = new MondValue(_state);
            obj.Prototype = MondValue.Null;

            obj["__get"] = new MondFunction((_, args) =>
            {
                if (args.Length != 2)
                    throw new MondRuntimeException("LocalObject.__get: requires 2 parameters");

                var name = (string)args[1];

                Func<MondValue> getter;
                Action<MondValue> setter;

                if (!TryGetLocalAccessor(name, out getter, out setter))
                    throw new MondRuntimeException("`{0}` is not defined", name);

                return getter();
            });

            obj["__set"] = new MondFunction((_, args) =>
            {
                if (args.Length != 3)
                    throw new MondRuntimeException("LocalObject.__set: requires 3 parameters");

                var name = (string)args[1];
                var value = args[2];

                Func<MondValue> getter;
                Action<MondValue> setter;

                if (!TryGetLocalAccessor(name, out getter, out setter))
                    throw new MondRuntimeException("`{0}` is not defined", name);

                if (setter == null)
                    throw new MondRuntimeException("`{0}` is read-only", name);

                setter(value);
                return value;
            });

            return obj;
        }

        private bool TryGetLocalAccessor(string name, out Func<MondValue> getter, out Action<MondValue> setter)
        {
            getter = null;
            setter = null;

            if (string.IsNullOrEmpty(name) || DebugInfo == null || DebugInfo.Scopes == null || _locals == null)
                return false;

            var scope = DebugInfo.FindScope(_address);
            if (scope == null)
                return false;

            var scopes = DebugInfo.Scopes;
            var id = scope.Id;

            do
            {
                scope = scopes[id];

                var identifiers = scope.Identifiers;

                foreach (var ident in identifiers)
                {
                    if (Program.Strings[ident.Name] != name)
                        continue;

                    var frameIndex = ident.FrameIndex;
                    var localId = ident.Id;

                    if (ident.FrameIndex >= 0)
                    {
                        getter = () => _locals.Get(frameIndex, localId);
                        setter = value => _locals.Set(frameIndex, localId, value);
                    }
                    else
                    {
                        getter = () => _args.Get(-frameIndex, localId);
                        setter = value => _args.Set(-frameIndex, localId, value);
                    }

                    if (ident.IsReadOnly)
                        setter = null;

                    return true;
                }

                id = scope.ParentId;
            } while (id >= 0);

            return false;
        }

        private List<CallStackEntry> GenerateCallStack(int address, ReturnAddress[] callStack, int callStackTop, int callStackBottom)
        {
            var result = new List<CallStackEntry>();

            // current location
            result.Add(GenerateCallStackEntry(Program, address));

            // previous locations
            for (var i = callStackTop - 1; i >= 0; i--)
            {
                if (i == callStackBottom)
                {
                    if (callStackBottom > 0)
                        result.Add(new CallStackEntry(0, "[... native ...]", null, 0, -1));

                    continue;
                }

                var returnAddress = callStack[i];
                result.Add(GenerateCallStackEntry(returnAddress.Program, returnAddress.Address));
            }

            return result;
        }

        private static CallStackEntry GenerateCallStackEntry(MondProgram program, int address)
        {
            var debugInfo = program.DebugInfo;

            if (debugInfo == null)
                return new CallStackEntry(address, program.GetHashCode().ToString("X8"), address.ToString("X8"), 0, -1);

            var fileName = program.DebugInfo.FileName;
            string function = null;
            var lineNumber = 0;
            var columnNumber = -1;

            var func = program.DebugInfo.FindFunction(address);
            if (func.HasValue)
                function = program.Strings[func.Value.Name];

            var position = program.DebugInfo.FindPosition(address);
            if (position.HasValue)
            {
                lineNumber = position.Value.LineNumber;
                columnNumber = position.Value.ColumnNumber;
            }

            if (fileName == null)
                fileName = program.GetHashCode().ToString("X8");

            if (function == null)
                function = address.ToString("X8");

            return new CallStackEntry(address, fileName, function, lineNumber, columnNumber);
        }

        public class CallStackEntry
        {
            public readonly int Address;
            public readonly string FileName;
            public readonly string Function;
            public readonly int LineNumber;
            public readonly int ColumnNumber;

            internal CallStackEntry(int address, string fileName, string function, int lineNumber, int columnNumber)
            {
                Address = address;
                FileName = fileName;
                Function = function;
                LineNumber = lineNumber;
                ColumnNumber = columnNumber;
            }
        }
    }
}

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
        private readonly MondValue[] _locals;
        private readonly ReturnAddress _args;

        private readonly Dictionary<string, (Func<MondValue> Getter, Action<MondValue> Setter)> _localAccessors;
        private readonly MondValue _localObject;

        public MondProgram Program { get; }
        public ReadOnlyCollection<CallStackEntry> CallStack { get; }

        internal MondDebugContext(
            MondState state, MondProgram program, int address,
            MondValue[] locals, ReturnAddress args,
            ReturnAddress[] callStack, int callStackTop, int callStackBottom)
        {
            _state = state;
            _address = address;
            _locals = locals;
            _args = args;

            Program = program;
            CallStack = GenerateCallStack(address, callStack, callStackTop, callStackBottom).AsReadOnly();

            _localAccessors = GetLocalAccessors();
            _localObject = CreateLocalObject();
        }

        public MondValue GetLocals()
        {
            var result = MondValue.Object();
            var globalDict = _state.Global.AsDictionary;

            foreach (var kvp in _localAccessors)
            {
                var name = kvp.Key;
                var localValue = kvp.Value.Getter();
                var globalValue = _state[name];

                if (!globalDict.ContainsKey(name) || localValue != globalValue)
                {
                    result[name] = localValue;
                }
            }

            return result;
        }

        public MondValue Evaluate(string expression)
        {
            var options = new MondCompilerOptions();

            var source = "return " + expression;
            var lexer = new Lexer(source, "debug", options);
            var parser = new Parser(lexer);

            var expr = parser.ParseStatement(false);

            var rewriter = new DebugExpressionRewriter(LocalObjectName);
            expr = expr.Accept(rewriter);

            var oldLocal = _state[LocalObjectName];
            _state[LocalObjectName] = _localObject;

            try
            {
                var program = new ExpressionCompiler(options).Compile(expr, "debug-expr", source);
                return _state.Load(program);
            }
            finally
            {
                _state[LocalObjectName] = oldLocal;
            }
        }

        private MondValue CreateLocalObject()
        {
            var proxyHandler = MondValue.Object(_state);
            proxyHandler.Prototype = MondValue.Null;
            
            proxyHandler["get"] = new MondFunction((_, args) =>
            {
                if (args.Length != 2)
                    throw new MondRuntimeException("LocalObject.get: requires 2 parameters");

                var name = (string)args[1];
                
                if (!_localAccessors.TryGetValue(name, out var accessors))
                    throw new MondRuntimeException("`{0}` is not defined", name);

                return accessors.Getter();
            });

            proxyHandler["set"] = new MondFunction((_, args) =>
            {
                if (args.Length != 3)
                    throw new MondRuntimeException("LocalObject.set: requires 3 parameters");

                var name = (string)args[1];
                var value = args[2];
                
                if (!_localAccessors.TryGetValue(name, out var accessors))
                    throw new MondRuntimeException("`{0}` is not defined", name);

                if (accessors.Setter == null)
                    throw new MondRuntimeException("`{0}` is read-only", name);

                accessors.Setter(value);
                return value;
            });

            var target = MondValue.Object(_state);
            var obj = MondValue.ProxyObject(target, proxyHandler, _state);
            return obj;
        }

        private Dictionary<string, (Func<MondValue>, Action<MondValue>)> GetLocalAccessors()
        {
            var result = new Dictionary<string, (Func<MondValue>, Action<MondValue>)>();

            var debugInfo = Program.DebugInfo;
            if (debugInfo?.Scopes == null || _locals == null)
                return result;

            var scope = debugInfo.FindScope(_address);
            if (scope == null)
                return result;

            var currentFrameIndex = scope.FrameIndex;
            var scopes = debugInfo.Scopes;
            var id = scope.Id;

            do
            {
                scope = scopes[id];
                
                var frameIndex = scope.FrameIndex;

                var identifiers = scope.Identifiers;
                foreach (var ident in identifiers)
                {
                    var name = Program.Strings[ident.Name];
                    if (result.ContainsKey(name))
                        continue;

                    var localId = ident.Id;

                    Func<MondValue> getter;
                    Action<MondValue> setter;

                    if (ident.IsCaptured)
                    {
                        getter = () => _args.Closure.Upvalues[frameIndex][localId];
                        setter = value => _args.Closure.Upvalues[frameIndex][localId] = value;
                    }
                    else if (frameIndex != currentFrameIndex)
                    {
                        continue; // not accessible from this frame
                    }
                    else if (ident.IsArgument)
                    {
                        getter = () => _args.GetArgument(localId);
                        setter = value => _args.SetArgument(localId, value);
                    }
                    else
                    {
                        getter = () => _locals[localId];
                        setter = value => _locals[localId] = value;
                    }

                    if (ident.IsReadOnly)
                        setter = null;

                    result.Add(name, (getter, setter));
                }

                id = scope.ParentId;
            } while (id >= 0);

            return result;
        }

        private List<CallStackEntry> GenerateCallStack(int address, ReturnAddress[] callStack, int callStackTop, int callStackBottom)
        {
            var result = new List<CallStackEntry>();

            // current location
            result.Add(GenerateCallStackEntry(Program, address));

            // previous locations
            for (var i = callStackTop; i >= 0; i--)
            {
                if (i == callStackBottom)
                {
                    if (callStackBottom > 0)
                        result.Add(new CallStackEntry(null, 0, "[... native ...]", null, 0, -1));

                    continue;
                }

                var returnAddress = callStack[i];
                if (returnAddress.IsEntry)
                {
                    continue;
                }

                result.Add(GenerateCallStackEntry(returnAddress.Program, returnAddress.Address));
            }

            return result;
        }

        private static CallStackEntry GenerateCallStackEntry(MondProgram program, int address)
        {
            var debugInfo = program.DebugInfo;

            if (debugInfo == null)
            {
                return new CallStackEntry(
                    program, address, program.GetHashCode().ToString("X8"), address.ToString("X8"), 0, -1);
            }

            var fileName = program.DebugInfo.FileName;
            string function = null;
            var startLineNumber = 0;
            var startColumnNumber = -1;
            int? endLineNumber = null;
            int? endColumnNumber = null;

            var func = program.DebugInfo.FindFunction(address);
            if (func.HasValue)
                function = program.Strings[func.Value.Name];

            var statement = program.DebugInfo.FindStatement(address);
            if (statement.HasValue)
            {
                startLineNumber = statement.Value.StartLineNumber;
                startColumnNumber = statement.Value.StartColumnNumber;
                endLineNumber = statement.Value.EndLineNumber;
                endColumnNumber = statement.Value.EndColumnNumber;
            }
            else
            {
                var position = program.DebugInfo.FindPosition(address);
                if (position.HasValue)
                {
                    startLineNumber = position.Value.LineNumber;
                    startColumnNumber = position.Value.ColumnNumber;
                }
            }

            if (fileName == null)
                fileName = program.GetHashCode().ToString("X8");

            if (function == null)
                function = address.ToString("X8");
            else if (string.IsNullOrEmpty(function))
                function = "<top level>";

            return new CallStackEntry(
                program, address, fileName, function, startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
        }

        public class CallStackEntry
        {
            public MondProgram Program { get; }
            public int Address { get; }
            public string FileName { get; }
            public string Function { get; }
            public int StartLineNumber { get; }
            public int StartColumnNumber { get; }
            public int? EndLineNumber { get; }
            public int? EndColumnNumber { get; }

            internal CallStackEntry(
                MondProgram program,
                int address,
                string fileName,
                string function,
                int startLineNumber,
                int startColumnNumber,
                int? endLineNumber = null,
                int? endColumnNumber = null)
            {
                Program = program;
                Address = address;
                FileName = fileName;
                Function = function;
                StartLineNumber = startLineNumber;
                StartColumnNumber = startColumnNumber;
                EndLineNumber = endLineNumber;
                EndColumnNumber = endColumnNumber;
            }
        }
    }
}

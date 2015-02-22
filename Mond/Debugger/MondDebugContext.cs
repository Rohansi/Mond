using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mond.VirtualMachine;

namespace Mond.Debugger
{
    public class MondDebugContext
    {
        private readonly int _address;
        private readonly MondValue _globals;
        private readonly Frame _locals;

        public readonly MondProgram Program;
        public readonly MondDebugInfo DebugInfo;

        public readonly ReadOnlyCollection<CallStackEntry> CallStack;

        internal MondDebugContext(
            MondProgram program, MondDebugInfo debugInfo, int address,
            MondValue globals, Frame locals,
            ReturnAddress[] callStack,  int callStackTop, int callStackBottom)
        {
            _address = address;
            _globals = globals;
            _locals = locals;

            Program = program;
            DebugInfo = debugInfo;

            CallStack = GenerateCallStack(address, callStack, callStackTop, callStackBottom).AsReadOnly();
        }

        public bool TryGetLocal(string name, out MondValue value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(name) || DebugInfo == null || DebugInfo.Scopes == null)
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

                    value = _locals.Get(ident.FrameIndex, ident.Id);
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

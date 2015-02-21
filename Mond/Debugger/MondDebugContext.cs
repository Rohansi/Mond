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

        internal MondDebugContext(MondProgram program, MondDebugInfo debugInfo, int address, MondValue globals, Frame locals)
        {
            _address = address;
            _globals = globals;
            _locals = locals;

            Program = program;
            DebugInfo = debugInfo;
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
    }
}

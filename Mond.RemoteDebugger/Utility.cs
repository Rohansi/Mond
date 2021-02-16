using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal static class Utility
    {
        public static int? GetInt(this MondValue obj, string fieldName)
        {
            var field = obj[fieldName];
            return field.Type == MondValueType.Number
                ? (int)field
                : null;
        }

        public static MondValue JsonBreakpoint(MondDebugInfo.Statement statement)
        {
            var obj = MondValue.Object();
            obj["address"] = statement.Address;
            obj["line"] = statement.StartLineNumber;
            obj["column"] = statement.StartColumnNumber;
            obj["endLine"] = statement.EndLineNumber;
            obj["endColumn"] = statement.EndColumnNumber;
            return obj;
        }

        public static MondValue JsonCallStackEntry(int programId, MondDebugContext.CallStackEntry callStackEntry)
        {
            var obj = MondValue.Object();
            obj["programId"] = programId;
            obj["address"] = callStackEntry.Address;
            obj["fileName"] = callStackEntry.FileName;
            obj["function"] = callStackEntry.Function;
            obj["line"] = callStackEntry.StartLineNumber;
            obj["column"] = callStackEntry.StartColumnNumber;
            obj["endLine"] = callStackEntry.EndLineNumber ?? MondValue.Undefined;
            obj["endColumn"] = callStackEntry.EndColumnNumber ?? MondValue.Undefined;
            return obj;
        }
    }
}

using System.Linq;
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

        public static MondValue JsonValueProperties(MondValue value)
        {
            switch (value.Type)
            {
                case MondValueType.Object:
                    var objProperties = value.AsDictionary
                        .Where(kvp => IsPrimitive(kvp.Key))
                        .Select(kvp => JsonValueProperty(kvp.Key, kvp.Value));
                    return MondValue.Array(objProperties);

                case MondValueType.Array:
                    var arrayProperties = value.AsList
                        .Select((v, i) => JsonValueProperty(i, v));
                    return MondValue.Array(arrayProperties);

                default:
                    return MondValue.Array();
            }
        }

        private static MondValue JsonValueProperty(MondValue key, MondValue value)
        {
            var property = MondValue.Object();
            property["name"] = key.ToString();
            property["nameType"] = key.Type.GetName();
            property["value"] = value.ToString();
            property["valueType"] = value.Type.GetName();
            return property;
        }

        private static bool IsPrimitive(in MondValue value)
        {
            return value.Type != MondValueType.Object &&
                   value.Type != MondValueType.Array &&
                   value.Type != MondValueType.Function;
        }
    }
}

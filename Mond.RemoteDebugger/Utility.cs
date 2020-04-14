using System.Linq;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal static class Utility
    {
        public static int FirstLineNumber(MondDebugInfo debugInfo)
        {
            var lines = debugInfo.Lines;
            var firstLineNumber = lines.Count > 0 ? lines[0].LineNumber : 1;
            return firstLineNumber;
        }

        public static MondValue JsonProgram(ProgramInfo program)
        {
            var obj = MondValue.Object();
            obj["FileName"] = program.FileName;
            obj["SourceCode"] = program.DebugInfo.SourceCode;
            obj["FirstLine"] = FirstLineNumber(program.DebugInfo);
            obj["Breakpoints"] = MondValue.Array(program.Breakpoints.Select(e => MondValue.Number(e)));
            return obj;
        }

        public static MondValue JsonWatch(Watch watch)
        {
            var obj = MondValue.Object();
            obj["Id"] = watch.Id;
            obj["Expression"] = watch.Expression;
            obj["Value"] = watch.Value;
            return obj;
        }

        public static MondValue JsonCallStackEntry(int programId, MondDebugContext.CallStackEntry callStackEntry)
        {
            var obj = MondValue.Object();
            obj["ProgramId"] = programId;
            obj["FileName"] = callStackEntry.FileName;
            obj["Function"] = callStackEntry.Function;
            obj["LineNumber"] = callStackEntry.LineNumber;
            obj["ColumnNumber"] = callStackEntry.ColumnNumber;
            return obj;
        }
    }
}

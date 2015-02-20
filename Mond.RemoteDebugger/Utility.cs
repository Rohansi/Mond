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
    }
}

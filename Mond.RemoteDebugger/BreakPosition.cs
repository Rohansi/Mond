namespace Mond.RemoteDebugger
{
    internal readonly struct BreakPosition
    {
        public int ProgramId { get; }
        public string ProgramPath { get; }
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }

        public BreakPosition(int programId, string programPath, int startLine, int startColumn, int endLine, int endColumn)
        {
            ProgramId = programId;
            ProgramPath = programPath;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }
}

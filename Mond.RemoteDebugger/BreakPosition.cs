namespace Mond.RemoteDebugger
{
    internal struct BreakPosition
    {
        public readonly int Id;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public BreakPosition(int id, int startLine, int startColumn, int endLine, int endColumn)
        {
            Id = id;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }
}

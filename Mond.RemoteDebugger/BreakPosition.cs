namespace Mond.RemoteDebugger
{
    internal struct BreakPosition
    {
        public int Id { get; }
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }

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

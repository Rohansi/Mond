using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Debugger
{
    public class MondDebugInfo
    {
        public readonly struct Function
        {
            public int Address { get; }
            public int Name { get; }

            public Function(int address, int name)
            {
                Address = address;
                Name = name;
            }
        }

        public readonly struct Position
        {
            public int Address { get; }
            public int LineNumber { get; }
            public int ColumnNumber { get; }

            public Position(int address, int lineNumber, int columnNumber)
            {
                Address = address;
                LineNumber = lineNumber;
                ColumnNumber = columnNumber;
            }
        }

        public readonly struct Statement
        {
            public int Address { get; }
            public int StartLineNumber { get; }
            public int StartColumnNumber { get; }
            public int EndLineNumber { get; }
            public int EndColumnNumber { get; }

            public Statement(int address, int startLine, int startColumn, int endLine, int endColumn)
            {
                Address = address;
                StartLineNumber = startLine;
                StartColumnNumber = startColumn;
                EndLineNumber = endLine;
                EndColumnNumber = endColumn;
            }
        }

        public class Scope
        {
            public int Id { get; }
            public int Depth { get; }
            public int ParentId { get; }
            public int StartAddress { get; }
            public int EndAddress { get; }
            public ReadOnlyCollection<Identifier> Identifiers;

            public Scope(int id, int depth, int parentId, int startAddress, int endAddress, List<Identifier> identifiers)
            {
                Id = id;
                Depth = depth;
                ParentId = parentId;
                StartAddress = startAddress;
                EndAddress = endAddress;
                Identifiers = identifiers?.AsReadOnly();
            }
        }

        public readonly struct Identifier
        {
            public int Name { get; }
            public bool IsReadOnly { get; }
            public int FrameIndex { get; }
            public int Id { get; }

            public Identifier(int name, bool isReadOnly, int frameIndex, int id)
            {
                Name = name;
                IsReadOnly = isReadOnly;
                FrameIndex = frameIndex;
                Id = id;
            }
        }

        private readonly List<Function> _functions;
        private readonly List<Position> _lines;
        private readonly List<Statement> _statements; 
        private readonly List<Scope> _scopes;

        private List<List<Scope>> _unpackedScopes;

        public string FileName { get; }
        public string SourceCode { get; }

        public ReadOnlyCollection<Function> Functions => _functions?.AsReadOnly();
        public ReadOnlyCollection<Position> Lines => _lines?.AsReadOnly();
        public ReadOnlyCollection<Statement> Statements => _statements?.AsReadOnly();
        public ReadOnlyCollection<Scope> Scopes => _scopes?.AsReadOnly();

        internal MondDebugInfo(string fileName, string sourceCode, List<Function> functions, List<Position> lines, List<Statement> statements, List<Scope> scopes)
        {
            FileName = fileName != "" ? fileName : null;
            SourceCode = sourceCode != "" ? sourceCode : null;

            _functions = functions;
            _lines = lines;
            _statements = statements;
            _scopes = scopes;
        }

        public Function? FindFunction(int address)
        {
            if (_functions == null)
                return null;

            var idx = Search(_functions, new Function(address, 0), FunctionAddressComparer);
            Function? result = null;

            if (idx >= 0 && idx < _functions.Count)
                result = _functions[idx];

            return result;
        }

        public Position? FindPosition(int address)
        {
            if (_lines == null)
                return null;

            var idx = Search(_lines, new Position(address, 0, 0), PositionAddressComparer);
            Position? result = null;

            if (idx >= 0 && idx < _lines.Count)
                result = _lines[idx];

            return result;
        }

        public Statement? FindStatement(int address)
        {
            if (_statements == null)
                return null;

            var search = new Statement(address, 0, 0, 0, 0);
            var idx = Search(_statements, search, StatementAddressComparer);
            Statement? result = null;

            if (idx >= 0 && idx < _statements.Count)
                result = _statements[idx];

            return result;
        }

        public IEnumerable<Statement> FindStatements(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (_statements == null)
                return Enumerable.Empty<Statement>();

            // TODO: can we optimize? statement list isn't guaranteed to be sorted by line...
            return _statements
                .Where(s => (s.StartLineNumber == startLine && s.StartColumnNumber >= startColumn) ||
                            (s.EndLineNumber == endLine && s.EndColumnNumber <= endColumn) ||
                            (s.StartLineNumber > startLine && s.EndLineNumber < endLine));
        }

        public bool IsStatementStart(int address)
        {
            if (_statements == null)
                return false;

            var search = new Statement(address, 0, 0, 0, 0);
            return _statements.BinarySearch(search, StatementAddressComparer) >= 0;
        }

        public Scope FindScope(int address)
        {
            if (_scopes == null)
                return null;

            if (_unpackedScopes == null)
            {
                _unpackedScopes = new List<List<Scope>>(16);

                for (var i = 0; i < _scopes.Count; i++)
                {
                    var scope = _scopes[i];

                    if (scope.Id != i)
                        throw new Exception();

                    while (scope.Depth >= _unpackedScopes.Count)
                    {
                        _unpackedScopes.Add(new List<Scope>(16));
                    }

                    _unpackedScopes[scope.Depth].Add(scope);
                }

                foreach (var d in _unpackedScopes)
                {
                    d.Sort(ScopeAddressSortComparer);
                }
            }

            // TODO: this can be faster
            for (var i = _unpackedScopes.Count - 1; i >= 0; i--)
            {
                var scopes = _unpackedScopes[i];

                for (var j = 0; j < scopes.Count; j++)
                {
                    if (address >= scopes[j].StartAddress && address <= scopes[j].EndAddress)
                        return scopes[j];
                }
            }

            return null;
        }

        private static int Search<T>(List<T> list, T key, IComparer<T> comparer)
        {
            var idx = list.BinarySearch(key, comparer);

            if (idx < 0)
                idx = ~idx - 1;

            return idx;
        }

        private static readonly GenericComparer<Function> FunctionAddressComparer =
            new GenericComparer<Function>((x, y) => x.Address - y.Address);

        private static readonly GenericComparer<Position> PositionAddressComparer =
            new GenericComparer<Position>((x, y) => x.Address - y.Address);

        private static readonly GenericComparer<Statement> StatementAddressComparer =
            new GenericComparer<Statement>((x, y) => x.Address - y.Address);

        private static readonly GenericComparer<Scope> ScopeAddressSortComparer =
            new GenericComparer<Scope>((x, y) => x.StartAddress - y.StartAddress);
    }
}

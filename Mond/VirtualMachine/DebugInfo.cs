using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.VirtualMachine
{
    class DebugInfo
    {
        public struct Function
        {
            public readonly int Address;
            public readonly int Name;

            public Function(int address, int name)
            {
                Address = address;
                Name = name;
            }
        }

        public struct Position
        {
            public readonly int Address;
            public readonly int FileName;
            public readonly int LineNumber;
            public readonly int ColumnNumber;

            public Position(int address, int fileName, int lineNumber, int columnNumber)
            {
                Address = address;
                FileName = fileName;
                LineNumber = lineNumber;
                ColumnNumber = columnNumber;
            }
        }

        public class Scope
        {
            public readonly int Id;
            public readonly int Depth;
            public readonly int ParentId;
            public readonly int StartAddress;
            public readonly int EndAddress;
            public readonly ReadOnlyCollection<Identifier> Identifiers;

            public Scope(int id, int depth, int parentId, int startAddress, int endAddress, List<Identifier> identifiers)
            {
                Id = id;
                Depth = depth;
                ParentId = parentId;
                StartAddress = startAddress;
                EndAddress = endAddress;
                Identifiers = identifiers != null ? identifiers.AsReadOnly() : null;
            }
        }

        public struct Identifier
        {
            public readonly int Name;
            public readonly bool IsReadOnly;
            public readonly int FrameIndex;
            public readonly int Id;

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
        private readonly List<Scope> _scopes;

        private List<List<Scope>> _unpackedScopes; 

        internal ReadOnlyCollection<Function> Functions
        {
            get { return _functions.AsReadOnly(); }
        }

        internal ReadOnlyCollection<Position> Lines
        {
            get { return _lines.AsReadOnly(); }
        }

        internal ReadOnlyCollection<Scope> Scopes
        {
            get { return _scopes.AsReadOnly(); }
        }

        public DebugInfo(List<Function> functions, List<Position> lines, List<Scope> scopes)
        {
            _functions = functions ?? new List<Function>();
            _lines = lines ?? new List<Position>();
            _scopes = scopes ?? new List<Scope>();
        }

        public Function? FindFunction(int address)
        {
            var idx = Search(_functions, new Function(address, 0), FunctionAddressComparer);
            Function? result = null;

            if (idx >= 0 && idx < _functions.Count)
                result = _functions[idx];

            return result;
        }

        public Position? FindPosition(int address)
        {
            var idx = Search(_lines, new Position(address, 0, 0, 0), PositionAddressComparer);
            Position? result = null;

            if (idx >= 0 && idx < _lines.Count)
                result = _lines[idx];

            return result;
        }

        public Scope FindScope(int address)
        {
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
            }

            var target = new Scope(0, 0, 0, address, address, null);

            for (var i = _unpackedScopes.Count - 1; i >= 0; i--)
            {
                var idx = _unpackedScopes[i].BinarySearch(target, ScopeAddressComparer);

                if (idx < 0)
                    continue;

                return _unpackedScopes[i][idx];
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

        private static readonly GenericComparer<Scope> ScopeAddressComparer =
            new GenericComparer<Scope>((x, y) =>
            {
                if (x.StartAddress <= y.StartAddress && x.EndAddress >= y.EndAddress)
                    return 0;

                if (x.EndAddress < y.StartAddress)
                    return -1;

                return 1;
            });
    }
}

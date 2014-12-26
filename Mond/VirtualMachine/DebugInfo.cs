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

        private readonly List<Function> _functions;
        private readonly List<Position> _lines;

        internal ReadOnlyCollection<Function> Functions
        {
            get { return _functions.AsReadOnly(); }
        }

        internal ReadOnlyCollection<Position> Lines
        {
            get { return _lines.AsReadOnly(); }
        }

        public DebugInfo(List<Function> functions, List<Position> lines)
        {
            _functions = functions;
            _lines = lines;
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
    }
}

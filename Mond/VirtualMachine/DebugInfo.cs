using System.Collections.Generic;

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

        public struct Line
        {
            public readonly int Address;
            public readonly int FileName;
            public readonly int LineNumber;

            public Line(int address, int fileName, int lineNumber)
            {
                Address = address;
                FileName = fileName;
                LineNumber = lineNumber;
            }
        }

        private readonly List<Function> _functions;
        private readonly List<Line> _lines;

        public DebugInfo(List<Function> functions, List<Line> lines)
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

        public Line? FindLine(int address)
        {
            var idx = Search(_lines, new Line(address, 0, 0), LineAddressComparer);
            Line? result = null;

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

        private static readonly GenericComparer<Line> LineAddressComparer =
            new GenericComparer<Line>((x, y) => x.Address - y.Address);
    }
}

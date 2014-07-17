using System;

namespace Mond.VirtualMachine
{
    class Frame
    {
        public readonly int Depth;
        public readonly Frame Previous;
        public MondValue[] Values { get; private set; }

        public Frame(int depth, Frame previous, int valueCount)
        {
            Depth = depth;
            Previous = previous;
            Values = new MondValue[valueCount];
        }

        public MondValue Get(int depth, int index)
        {
            var current = this;

            while (current.Depth > depth)
            {
                current = current.Previous;
            }

            if (index < 0 || index >= current.Values.Length)
                return MondValue.Undefined;

            return current.Values[index];
        }

        public void Set(int depth, int index, MondValue value)
        {
            var current = this;

            while (current.Depth > depth)
            {
                current = current.Previous;
            }

            if (index < 0)
                return;

            var values = current.Values;

            if (index >= values.Length)
                Array.Resize(ref values, index + 1);

            values[index] = value;
        }
    }
}

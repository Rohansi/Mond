using System;

namespace Mond.VirtualMachine
{
    class Frame
    {
        public readonly int Depth;
        public readonly Frame Previous;
        public MondValue[] Values;

        public Frame(int depth, Frame previous, int valueCount)
        {
            Depth = depth;
            Previous = previous;
            Values = new MondValue[valueCount];

            for (var i = 0; i < valueCount; i++)
            {
                Values[i] = MondValue.Undefined;
            }
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
            {
                var oldLength = values.Length;
                var newLength = index + 1;

                Array.Resize(ref values, newLength);
                current.Values = values;

                for (var i = oldLength; i < newLength; i++)
                {
                    values[i] = MondValue.Undefined;
                }
            }

            values[index] = value;
        }
    }
}

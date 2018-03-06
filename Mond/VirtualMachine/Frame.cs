using System;
using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Frame
    {
        public readonly int Depth;
        public readonly Frame Previous;
        public MondValue[] Values;

        public Frame StoredFrame;
        public List<MondValue> StoredEvals; 

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

        public ref readonly MondValue Get(int depth, int index)
        {
            var frame = GetFrame(depth);

            if (index < 0 || index >= frame.Values.Length)
                return ref MondValue.Undefined;

            return ref frame.Values[index];
        }

        public void Set(int depth, int index, in MondValue value)
        {
            var frame = GetFrame(depth);

            if (index < 0)
                return;

            var values = frame.Values;

            if (index >= values.Length)
            {
                var oldLength = values.Length;
                var newLength = index + 1;

                Array.Resize(ref values, newLength);
                frame.Values = values;

                for (var i = oldLength; i < newLength; i++)
                {
                    values[i] = MondValue.Undefined;
                }
            }

            values[index] = value;
        }

        public Frame GetFrame(int depth)
        {
            var current = this;

            while (current.Depth > depth)
            {
                current = current.Previous;
            }

            return current;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mond.VirtualMachine
{
    internal class Frame
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
            Values = valueCount > 0 ? new MondValue[valueCount] : [];
        }

        public Frame(int depth, Frame previous, Span<MondValue> values)
        {
            Depth = depth;
            Previous = previous;
            Values = values.Length == 0 ? [] : values.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly MondValue Get(int index)
        {
            if (index < 0 || index >= Values.Length)
                return ref MondValue.Undefined;

            return ref Values[index];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly MondValue Get(int depth, int index)
        {
            var frame = GetFrame(depth);
            return ref frame.Get(index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, in MondValue value)
        {
            if (index < 0)
                return;
                
            if (index >= Values.Length)
            {
                var newLength = index + 1;
                Array.Resize(ref Values, newLength);
            }

            Values[index] = value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int depth, int index, in MondValue value)
        {
            var frame = GetFrame(depth);
            frame.Set(index, in value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

using System;
using System.Runtime.CompilerServices;

namespace Mond.VirtualMachine
{
    internal readonly struct Frame
    {
        public readonly MondValue[] Values;

        public Frame(MondValue[] values)
        {
            Values = values ?? [];
        }
        
        public Frame(int valueCount)
        {
            Values = valueCount > 0 ? new MondValue[valueCount] : [];
        }

        public Frame(Span<MondValue> values)
        {
            Values = values.Length == 0 ? [] : values.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly MondValue Get(int index)
        {
            return ref Values[index];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, in MondValue value)
        {
            Values[index] = value;
        }
    }
}

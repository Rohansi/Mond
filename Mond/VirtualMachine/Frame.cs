namespace Mond.VirtualMachine
{
    class Frame
    {
        public readonly int Depth;
        public readonly Frame Previous;
        public readonly MondValue[] Values;

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

            if (index < 0 || index >= current.Values.Length)
                return;

            current.Values[index] = value;
        }
    }
}

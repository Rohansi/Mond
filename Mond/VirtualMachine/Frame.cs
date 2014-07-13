namespace Mond.VirtualMachine
{
    class Frame
    {
        public readonly int Depth;
        public readonly Frame Previous;
        public readonly MondValue[] Values;

        public Frame(int depth, Frame previous, int localCount)
        {
            Depth = depth;
            Previous = previous;
            Values = new MondValue[localCount];
        }

        public MondValue Get(int depth, int index)
        {
            var current = this;

            while (current.Depth > depth)
            {
                current = current.Previous;
            }

            return current.Values[index];
        }

        public void Set(int depth, int index, MondValue value)
        {
            var current = this;

            while (current.Depth > depth)
            {
                current = current.Previous;
            }

            current.Values[index] = value;
        }
    }
}

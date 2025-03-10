namespace Mond.VirtualMachine
{
    internal enum ClosureType
    {
        Native, Mond
    }

    internal class Closure
    {
        public readonly ClosureType Type;

        public readonly MondProgram Program;
        public readonly int Address;
        public readonly MondValue[] Upvalues; // note: all MondValues should be arrays!

        public readonly MondFunction NativeFunction;

        public Closure(MondProgram program, int address, MondValue[] upvalues)
        {
            Type = ClosureType.Mond;
            
            Program = program;
            Address = address;
            Upvalues = upvalues;
        }

        public Closure(MondFunction function)
        {
            Type = ClosureType.Native;

            NativeFunction = function;
        }
    }
}

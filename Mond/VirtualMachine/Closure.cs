namespace Mond.VirtualMachine
{
    internal enum ClosureType
    {
        Native, Mond
    }

    internal class Closure
    {
        public ClosureType Type => NativeFunction != null ? ClosureType.Native : ClosureType.Mond;

        public readonly MondProgram Program;
        public readonly int Address;
        public readonly Frame Arguments;
        public readonly Frame Locals;

        public readonly MondFunction NativeFunction;

        public Closure(MondProgram program, int address, Frame arguments, Frame locals)
        {
            Program = program;
            Address = address;
            Arguments = arguments;
            Locals = locals;
        }

        public Closure(MondFunction function)
        {
            NativeFunction = function;
        }
    }
}

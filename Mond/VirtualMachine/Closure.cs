namespace Mond.VirtualMachine
{
    enum ClosureType
    {
        Native, Mond
    }

    class Closure
    {
        public readonly ClosureType Type;

        public readonly MondProgram Program;
        public readonly int Address;
        public readonly Frame Arguments;
        public readonly Frame Locals;

        public readonly MondFunction NativeFunction;

        public Closure(MondProgram program, int address, Frame arguments, Frame locals)
        {
            Type = ClosureType.Mond;
            
            Program = program;
            Address = address;
            Arguments = arguments;
            Locals = locals;
        }

        public Closure(MondFunction function)
        {
            Type = ClosureType.Native;

            NativeFunction = function;
        }
    }
}

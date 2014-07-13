namespace Mond.VirtualMachine
{
    enum ClosureType
    {
        Native, Mond
    }

    class Closure
    {
        public readonly ClosureType Type;

        public readonly int ProgramId;
        public readonly int Address;
        public readonly Frame Arguments;
        public readonly Frame Locals;

        public readonly MondFunction NativeFunction;

        public Closure(int programId, int address, Frame arguments, Frame locals)
        {
            Type = ClosureType.Mond;
            
            ProgramId = programId;
            Address = address;
            Arguments = arguments;
            Locals = locals;

            NativeFunction = null;
        }

        public Closure(MondFunction function)
        {
            Type = ClosureType.Native;

            NativeFunction = function;

            ProgramId = -1;
            Address = -1;
            Arguments = null;
            Locals = null;
        }
    }
}

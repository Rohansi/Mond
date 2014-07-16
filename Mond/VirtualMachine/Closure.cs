namespace Mond.VirtualMachine
{
    enum ClosureType
    {
        Native, InstanceNative, Mond
    }

    class Closure
    {
        public readonly ClosureType Type;

        public readonly int ProgramId;
        public readonly int Address;
        public readonly Frame Arguments;
        public readonly Frame Locals;

        public readonly MondFunction NativeFunction;
        public readonly MondInstanceFunction InstanceNativeFunction;

        public Closure(int programId, int address, Frame arguments, Frame locals)
        {
            Type = ClosureType.Mond;
            
            ProgramId = programId;
            Address = address;
            Arguments = arguments;
            Locals = locals;
        }

        public Closure(MondFunction function)
        {
            Type = ClosureType.Native;

            NativeFunction = function;
        }

        public Closure(MondInstanceFunction function)
        {
            Type = ClosureType.InstanceNative;

            InstanceNativeFunction = function;
        }
    }
}

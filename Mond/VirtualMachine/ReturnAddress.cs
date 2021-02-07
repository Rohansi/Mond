namespace Mond.VirtualMachine
{
    readonly struct ReturnAddress
    {
        public readonly MondProgram Program;
        public readonly int Address;

        public readonly Frame Arguments;

        public readonly int EvalDepth;

        public ReturnAddress(MondProgram program, int address, Frame arguments, int evalDepth)
        {
            Program = program;
            Address = address;
            Arguments = arguments;
            EvalDepth = evalDepth;
        }
    }
}

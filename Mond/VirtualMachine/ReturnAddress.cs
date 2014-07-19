namespace Mond.VirtualMachine
{
    struct ReturnAddress
    {
        public readonly MondProgram Program;
        public readonly int Address;
        public readonly Frame Arguments;

        public ReturnAddress(MondProgram program, int address, Frame arguments)
        {
            Program = program;
            Address = address;
            Arguments = arguments;
        }
    }
}

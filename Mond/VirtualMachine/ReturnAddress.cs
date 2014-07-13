namespace Mond.VirtualMachine
{
    struct ReturnAddress
    {
        public readonly int ProgramId;
        public readonly int Address;
        public readonly Frame Arguments;

        public ReturnAddress(int programId, int address, Frame arguments)
        {
            ProgramId = programId;
            Address = address;
            Arguments = arguments;
        }
    }
}

using Mond.VirtualMachine;

namespace Mond
{
    public delegate MondValue MondFunction(params MondValue[] arguments);

    public sealed class MondState
    {
        private Machine _machine;

        public MondState()
        {
            _machine = new Machine();
        }

        public MondValue this[MondValue index]
        {
            get { return _machine.Global[index]; }
            set { _machine.Global[index] = value; }
        }

        public MondValue Load(MondProgram program)
        {
            return _machine.Load(program);
        }

        public MondValue Call(MondValue closure, params MondValue[] arguments)
        {
            return _machine.Call(closure, arguments);
        }
    }
}

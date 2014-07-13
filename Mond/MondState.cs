using System.Linq;
using Mond.VirtualMachine;

namespace Mond
{
    public delegate MondValue MondFunction(MondState state, MondValue instance, params MondValue[] arguments);

    public class MondState
    {
        private Machine _machine;

        public MondState()
        {
            _machine = new Machine(this);
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

        public MondValue Call(MondValue closure, MondValue instance, params MondValue[] arguments)
        {
            arguments = Enumerable.Repeat(instance, 1).Concat(arguments).ToArray();
            return _machine.Call(closure, arguments);
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using Mond.Debugger;
using Mond.VirtualMachine;

[assembly:InternalsVisibleTo("Mond.Tests")]

namespace Mond
{
    public delegate MondValue MondFunction(MondState state, params MondValue[] arguments);
    public delegate MondValue MondInstanceFunction(MondState state, MondValue instance, params MondValue[] arguments);

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

        public MondDebugger Debugger
        {
            get { return _machine.Debugger; }
            set
            {
                if (_machine.Debugger != null)
                    _machine.Debugger.Detach();

                if (!value.TryAttach())
                    throw new InvalidOperationException("Debuggers cannot be attached to more than one state at a time");

                _machine.Debugger = value;
            }
        }

        public MondValue Load(MondProgram program)
        {
            return _machine.Load(program);
        }

        public MondValue Call(MondValue function, params MondValue[] arguments)
        {
            return _machine.Call(function, arguments);
        }
    }
}

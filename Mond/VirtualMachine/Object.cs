using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Object
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public MondValue Prototype;
        public object UserData;

        private MondState _dispatcherState;

        public MondState State
        {
            set
            {
                if (_dispatcherState == null)
                    _dispatcherState = value;
            }
        }

        public Object()
        {
            Values = new Dictionary<MondValue, MondValue>();
            Locked = false;
            Prototype = null;
            UserData = null;
        }

        public bool TryDispatch(string name, out MondValue result, params MondValue[] args)
        {
            MondState state = null;
            MondValue callable;

            var current = this;

            while (true)
            {
                if (current.Values.TryGetValue(name, out callable))
                {
                    // we need to use the state from the metamethod's object
                    state = current._dispatcherState;
                    break;
                }

                var currentValue = current.Prototype;

                if (currentValue == null || currentValue.Type != MondValueType.Object)
                    break;

                current = currentValue.ObjectValue;
            }

            if (callable == null)
            {
                result = MondValue.Undefined;
                return false;
            }

            if (state == null)
                throw new MondRuntimeException("MondValue must have an attached state to use metamethods");

            result = state.Call(callable, args);
            return true;
        }
    }
}

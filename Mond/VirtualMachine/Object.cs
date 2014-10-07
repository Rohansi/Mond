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
            if (_dispatcherState == null)
            {
                result = MondValue.Undefined;
                return false;
            }

            MondState state = null;
            MondValue callable;

            if (!Values.TryGetValue(name, out callable))
            {
                var current = Prototype;

                while (current != null && current.Type == MondValueType.Object)
                {
                    if (current.ObjectValue.Values.TryGetValue(name, out callable))
                    {
                        // we should use the state from the metamethod's object
                        state = current.ObjectValue._dispatcherState;
                        break;
                    }

                    current = current.Prototype;
                }
            }

            if (callable == null)
            {
                result = MondValue.Undefined;
                return false;
            }

            state = state ?? _dispatcherState;
            if (state == null)
                throw new MondRuntimeException("MondValue must have an attached state to use metamethods");

            result = state.Call(callable, args);
            return true;
        }
    }
}

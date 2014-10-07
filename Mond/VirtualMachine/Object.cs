using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Object
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public MondValue Prototype;
        public object UserData;

        private MondState _dispatcherState = null;

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

            MondValue callable = null;

            if (!Values.TryGetValue(name, out callable))
            {
                var current = Prototype;

                while (current != null && current.Type == MondValueType.Object)
                {
                    if (current.ObjectValue.Values.TryGetValue(name, out callable))
                        break;

                    current = current.Prototype;
                }
            }

            if (callable == null)
            {
                result = MondValue.Undefined;
                return false;
            }

            result = _dispatcherState.Call(callable, args);
            return true;
        }
    }
}

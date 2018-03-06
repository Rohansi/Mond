using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Object
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public bool HasPrototype;
        public MondValue Prototype;
        public object UserData;

        private MondState _dispatcherState;

        public MondState State
        {
            get { return _dispatcherState; }
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
            HasPrototype = false;
            Prototype = MondValue.Undefined;
            UserData = null;
        }
    }
}

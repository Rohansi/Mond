using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Object
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public MondValue Prototype;
        public object UserData;
        public bool ThisEnabled;

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
            Prototype = null;
            UserData = null;
            ThisEnabled = false;
        }
    }
}

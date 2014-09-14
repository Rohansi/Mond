using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class Object
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public MondValue Prototype;
        public object UserData;

        public Object()
        {
            Values = new Dictionary<MondValue, MondValue>();
            Locked = false;
            Prototype = null;
            UserData = null;
        }
    }
}

using System.Collections.Generic;

namespace Mond
{
    class MondObject
    {
        public readonly Dictionary<MondValue, MondValue> Values;
        public bool Locked;
        public MondValue Prototype;

        public MondObject()
        {
            Values = new Dictionary<MondValue, MondValue>();
            Locked = false;
        }
    }
}

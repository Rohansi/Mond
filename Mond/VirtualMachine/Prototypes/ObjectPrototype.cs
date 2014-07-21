using System;

namespace Mond.VirtualMachine.Prototypes
{
    static class ObjectPrototype
    {
        public static readonly MondValue Value;

        static ObjectPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = MondValue.Undefined; // required to break the chain

            Value["getType"] = new MondInstanceFunction(GetType);

            Value.Lock();
        }

        private static MondValue GetType(MondState state, MondValue instance, params MondValue[] args)
        {
            switch (instance.Type)
            {
                case MondValueType.Undefined:
                    return "undefined";

                case MondValueType.Null:
                    return "null";

                case MondValueType.True:
                case MondValueType.False:
                    return "bool";

                case MondValueType.Object:
                    return "object";

                case MondValueType.Array:
                    return "array";

                case MondValueType.Number:
                    return "number";

                case MondValueType.String:
                    return "string";

                case MondValueType.Closure:
                    return "closure";

                default:
                    throw new NotSupportedException();
            }
        }
    }
}

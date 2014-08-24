using System;

namespace Mond.Prototypes
{
    /// <summary>
    /// Contains members common to ALL values.
    /// </summary>
    static class ValuePrototype
    {
        public static readonly MondValue Value;

        static ValuePrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value.Prototype = MondValue.Null; // required to break the chain

            Value["getType"] = new MondInstanceFunction(GetType);

            Value.Lock();
        }

        /// <summary>
        /// getType(): string
        /// </summary>
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
                    return "function";

                default:
                    throw new NotSupportedException();
            }
        }
    }
}

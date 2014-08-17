using System;
using System.Collections.Generic;
using System.Linq;

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

            Value["length"] = new MondInstanceFunction(Length);
            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);

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

        /// <summary>
        /// Number length()
        /// </summary>
        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments);
            return instance.ObjectValue.Count;
        }

        /// <summary>
        /// Object getEnumerator()
        /// </summary>
        private static MondValue GetEnumerator(MondState state, MondValue instance, MondValue[] arguments)
        {
            Check("getEnumerator", instance.Type, arguments);

            var enumerator = new MondValue(MondValueType.Object);
            var keys = instance.ObjectValue.Keys.ToList();
            var i = 0;

            enumerator["current"] = MondValue.Null;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= keys.Count)
                    return false;

                var pair = new MondValue(MondValueType.Object);
                pair["key"] = keys[i];
                pair["value"] = instance.ObjectValue[keys[i]];
                
                enumerator["current"] = pair;
                i++;
                return true;
            });

            return enumerator;
        }

        private static void Check(string method, MondValueType type, IList<MondValue> arguments, params MondValueType[] requiredTypes)
        {
            if (type != MondValueType.Object)
                throw new MondRuntimeException("Object.{0} must be called on an Object", type);

            if (arguments.Count < requiredTypes.Length)
                throw new MondRuntimeException("Object.{0} must be called with {1} argument{2}", method, requiredTypes.Length, requiredTypes.Length == 1 ? "" : "s");

            for (var i = 0; i < requiredTypes.Length; i++)
            {
                if (requiredTypes[i] == MondValueType.Undefined)
                    continue;

                if (arguments[i].Type != requiredTypes[i])
                    throw new MondRuntimeException("Argument {1} in Object.{0} must be of type {2}", method, i + 1, requiredTypes[i]);
            }
        }
    }
}

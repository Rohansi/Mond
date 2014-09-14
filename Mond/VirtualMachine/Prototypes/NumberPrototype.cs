using System.Collections.Generic;

namespace Mond.VirtualMachine.Prototypes
{
    static class NumberPrototype
    {
        public static readonly MondValue Value;

        static NumberPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value.Prototype = ValuePrototype.Value;

            Value["isNaN"] = new MondInstanceFunction(IsNaN);

            Value.Lock();
        }

        private static MondValue IsNaN(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("isNaN", instance.Type, arguments);
            return double.IsNaN(instance);
        }

        private static void Check(string method, MondValueType type, IList<MondValue> arguments, params MondValueType[] requiredTypes)
        {
            if (type != MondValueType.Number)
                throw new MondRuntimeException("Number.{0}: must be called on a Number", method);

            if (arguments.Count < requiredTypes.Length)
                throw new MondRuntimeException("Number.{0}: must be called with {1} argument{2}", method, requiredTypes.Length, requiredTypes.Length == 1 ? "" : "s");

            for (var i = 0; i < requiredTypes.Length; i++)
            {
                if (requiredTypes[i] == MondValueType.Undefined)
                    continue;

                if (arguments[i].Type != requiredTypes[i])
                    throw new MondRuntimeException("Number.{0}: argument {1} must be of type {2}", method, i + 1, requiredTypes[i]);
            }
        }
    }
}

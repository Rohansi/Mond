namespace Mond.VirtualMachine.Prototypes
{
    static class ArrayPrototype
    {
        public static readonly MondValue Value;

        static ArrayPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = ObjectPrototype.Value;

            Value["length"] = new MondInstanceFunction(Length);
            Value["add"] = new MondInstanceFunction(Add);

            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);
        }

        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments.Length, 0);
            return instance.ArrayValue.Count;
        }

        private static MondValue Add(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("add", instance.Type, arguments.Length, 1);

            instance.ArrayValue.Add(arguments[0]);
            return instance;
        }

        private static MondValue GetEnumerator(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("getEnumerator", instance.Type, arguments.Length, 0);

            var enumerator = new MondValue(MondValueType.Object);
            var i = 0;

            enumerator["current"] = MondValue.Null;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= instance.ArrayValue.Count)
                    return false;

                enumerator["current"] = instance.ArrayValue[i++];
                return true;
            });

            return enumerator;
        }

        private static void Check(string method, MondValueType type, int argCount, int requiredArgCount)
        {
            if (type != MondValueType.Array)
                throw new MondRuntimeException("Array.{0} must be called on an Array", type);

            if (argCount < requiredArgCount)
                throw new MondRuntimeException("Array.{0} must be called with {1} argument{2}", method, requiredArgCount, requiredArgCount == 1 ? "" : "s");
        }
    }
}

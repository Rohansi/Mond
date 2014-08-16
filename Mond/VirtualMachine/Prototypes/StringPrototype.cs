namespace Mond.VirtualMachine.Prototypes
{
    static class StringPrototype
    {
        public static readonly MondValue Value;

        static StringPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = ObjectPrototype.Value;

            Value["length"] = new MondInstanceFunction(Length);
            Value["toUpper"] = new MondInstanceFunction(ToUpper);
            Value["toLower"] = new MondInstanceFunction(ToLower);
            
            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);

            Value.Lock();
        }

        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments.Length, 0);
            return ((string)instance).Length;
        }

        private static MondValue ToUpper(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toUpper", instance.Type, arguments.Length, 0);
            return ((string)instance).ToUpper();
        }

        private static MondValue ToLower(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toLower", instance.Type, arguments.Length, 0);
            return ((string)instance).ToLower();
        }

        private static MondValue GetEnumerator(MondState state, MondValue instance, MondValue[] arguments)
        {
            Check("getEnumerator", instance.Type, arguments.Length, 0);

            var enumerator = new MondValue(MondValueType.Object);
            var str = (string)instance;
            var i = 0;

            enumerator["current"] = MondValue.Null;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= str.Length)
                    return false;

                enumerator["current"] = str[i++];
                return true;
            });

            return enumerator;
        }

        private static void Check(string method, MondValueType type, int argCount, int requiredArgCount)
        {
            if (type != MondValueType.Array)
                throw new MondRuntimeException("String.{0} must be called on a String", type);

            if (argCount < requiredArgCount)
                throw new MondRuntimeException("String.{0} must be called with {1} argument{2}", method, requiredArgCount, requiredArgCount == 1 ? "" : "s");
        }
    }
}

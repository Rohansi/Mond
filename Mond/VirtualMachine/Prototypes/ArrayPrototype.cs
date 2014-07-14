namespace Mond.VirtualMachine.Prototypes
{
    static class ArrayPrototype
    {
        public static readonly MondValue Value;

        static ArrayPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = ObjectPrototype.Value;

            Value["length"] = new MondFunction(Length);
        }

        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            if (instance.Type != MondValueType.Array)
                throw new MondRuntimeException("Array.length must be called on a String");

            return instance.ArrayValue.Count;
        }
    }
}

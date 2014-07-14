namespace Mond.VirtualMachine.Prototypes
{
    static class ObjectPrototype
    {
        public static readonly MondValue Value;

        static ObjectPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = MondValue.Undefined; // required to break the chain
        }
    }
}

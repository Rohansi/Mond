namespace Mond.VirtualMachine.Prototypes
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
            return instance.Type.GetName();
        }
    }
}

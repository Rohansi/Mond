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

            // we dont use MondValue.Prototype here because this should not have a prototype
            Value.ObjectValue.Prototype = MondValue.Undefined;

            Value["getType"] = new MondInstanceFunction(GetType);
            Value["toString"] = new MondInstanceFunction(ToString);
            Value["serialize"] = new MondInstanceFunction(Serialize);
            Value["getPrototype"] = new MondInstanceFunction(GetPrototype);

            Value.Lock();
        }

        /// <summary>
        /// getType(): string
        /// </summary>
        private static MondValue GetType(MondState state, MondValue instance, params MondValue[] args)
        {
            return instance.Type.GetName();
        }

        /// <summary>
        /// toString(): string
        /// </summary>
        private static MondValue ToString(MondState state, MondValue instance, params MondValue[] args)
        {
            return instance.ToString();
        }

        /// <summary>
        /// serialize(): string
        /// </summary>
        private static MondValue Serialize(MondState state, MondValue instance, params MondValue[] args)
        {
            return instance.Serialize();
        }

        /// <summary>
        /// getPrototype(): object
        /// </summary>
        private static MondValue GetPrototype(MondState state, MondValue instance, params MondValue[] args)
        {
            return instance.Prototype;
        }
    }
}

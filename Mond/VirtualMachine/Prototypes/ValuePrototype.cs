using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    /// <summary>
    /// Contains members common to ALL values.
    /// </summary>
    [MondPrototype("Value")]
    internal static partial class ValuePrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static ValuePrototype()
        {
            ValueReadOnly = PrototypeObject.Build(MondValue.Undefined);
        }

        /// <summary>
        /// getType(): string
        /// </summary>
        [MondFunction]
        public static string GetType([MondInstance] MondValue instance)
        {
            return instance.Type.GetName();
        }

        /// <summary>
        /// toString(): string
        /// </summary>
        [MondFunction]
        public static string ToString([MondInstance] MondValue instance)
        {
            return instance.ToString();
        }

        /// <summary>
        /// serialize(): string
        /// </summary>
        [MondFunction]
        public static string Serialize([MondInstance] MondValue instance)
        {
            return instance.Serialize();
        }

        /// <summary>
        /// getPrototype(): object
        /// </summary>
        [MondFunction]
        public static MondValue GetPrototype([MondInstance] MondValue instance)
        {
            return instance.Prototype;
        }
    }
}

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
        /// Returns the name of the value's type, such as "number" or "object".
        /// </summary>
        [MondFunction]
        public static string GetType([MondInstance] MondValue instance)
        {
            return instance.Type.GetName();
        }

        /// <summary>
        /// Returns a human readable string for the value.
        /// </summary>
        [MondFunction]
        public static string ToString([MondInstance] MondValue instance)
        {
            return instance.ToString();
        }

        /// <summary>
        /// Returns the value written as Mond source, so it can be read back later.
        /// </summary>
        [MondFunction]
        public static string Serialize([MondInstance] MondValue instance)
        {
            return instance.Serialize();
        }

        /// <summary>
        /// Returns the object this value inherits its members from.
        /// </summary>
        [MondFunction]
        public static MondValue GetPrototype([MondInstance] MondValue instance)
        {
            return instance.Prototype;
        }
    }
}

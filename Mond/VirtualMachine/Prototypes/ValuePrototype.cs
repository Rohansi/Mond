using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    /// <summary>
    /// Contains members common to ALL values.
    /// </summary>
    [MondModule("Value")]
    internal static class ValuePrototype
    {
        public static readonly MondValue Value;

        static ValuePrototype()
        {
            Value = MondPrototypeBinder.Bind(typeof(ValuePrototype));

            // we dont use MondValue.Prototype here because this should not have a prototype
            Value.ObjectValue.Prototype = MondValue.Undefined;

            Value.Lock();
        }

        /// <summary>
        /// getType(): string
        /// </summary>
        [MondFunction("getType")]
        public static string GetType([MondInstance] MondValue instance)
        {
            return instance.Type.GetName();
        }

        /// <summary>
        /// toString(): string
        /// </summary>
        [MondFunction("toString")]
        public static string ToString([MondInstance] MondValue instance)
        {
            return instance.ToString();
        }

        /// <summary>
        /// serialize(): string
        /// </summary>
        [MondFunction("serialize")]
        public static string Serialize([MondInstance] MondValue instance)
        {
            return instance.Serialize();
        }

        /// <summary>
        /// getPrototype(): object
        /// </summary>
        [MondFunction("getPrototype")]
        public static MondValue GetPrototype([MondInstance] MondValue instance)
        {
            return instance.Prototype;
        }
    }
}

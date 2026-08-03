using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    /// <summary>
    /// Contains members available on every number.
    /// </summary>
    [MondPrototype("Number")]
    internal static partial class NumberPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static NumberPrototype()
        {
            ValueReadOnly = PrototypeObject.Build(ValuePrototype.Value);
        }

        /// <summary>
        /// Returns true when the number is the result of an undefined operation, such as 0 / 0.
        /// </summary>
        [MondFunction]
        public static MondValue IsNaN([MondInstance] MondValue instance)
        {
            return double.IsNaN(instance);
        }
    }
}

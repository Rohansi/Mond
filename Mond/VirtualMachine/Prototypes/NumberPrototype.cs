using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondPrototype("Number")]
    internal static partial class NumberPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static NumberPrototype()
        {
            ValueReadOnly = PrototypeObject.Build(ValuePrototype.Value);
        }

        [MondFunction]
        public static MondValue IsNaN([MondInstance] MondValue instance)
        {
            return double.IsNaN(instance);
        }
    }
}

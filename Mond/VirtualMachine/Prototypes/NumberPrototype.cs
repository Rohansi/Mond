using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Number")]
    internal static class NumberPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static NumberPrototype()
        {
            ValueReadOnly = MondPrototypeBinder.Bind(typeof(NumberPrototype));
            ValueReadOnly.Prototype = ValuePrototype.Value;

            ValueReadOnly.Lock();
        }

        [MondFunction]
        public static MondValue IsNaN([MondInstance] MondValue instance)
        {
            return double.IsNaN(instance);
        }
    }
}

using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Number")]
    internal static class NumberPrototype
    {
        public static readonly MondValue Value;

        static NumberPrototype()
        {
            Value = MondPrototypeBinder.Bind(typeof(NumberPrototype));
            Value.Prototype = ValuePrototype.Value;

            Value.Lock();
        }

        [MondFunction("isNaN")]
        public static MondValue IsNaN([MondInstance] MondValue instance)
        {
            return double.IsNaN(instance);
        }
    }
}

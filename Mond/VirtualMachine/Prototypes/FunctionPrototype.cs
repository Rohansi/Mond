using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Function")]
    internal static class FunctionPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static FunctionPrototype()
        {
            ValueReadOnly = MondPrototypeBinder.Bind(typeof(FunctionPrototype));
            ValueReadOnly.Prototype = ValuePrototype.Value;

            ValueReadOnly.Lock();
        }

        private const string MustBeAFunction = "Function.{0}: must be called on a function";

        /// <summary>
        /// getName(): string|undefined
        /// </summary>
        [MondFunction]
        public static MondValue GetName([MondInstance] MondValue instance)
        {
            EnsureFunction("getName", instance);

            var closure = instance.FunctionValue;
            if (closure.Type != ClosureType.Mond)
                return MondValue.Undefined;

            var program = closure.Program;
            var function = program.DebugInfo?.FindFunction(closure.Address);
            if (function == null)
                return MondValue.Undefined;

            return closure.Program.Strings[function.Value.Name];
        }

        private static void EnsureFunction(string methodName, MondValue instance)
        {
            if (instance.Type != MondValueType.Function)
                throw new MondRuntimeException(MustBeAFunction, methodName);
        }
    }
}

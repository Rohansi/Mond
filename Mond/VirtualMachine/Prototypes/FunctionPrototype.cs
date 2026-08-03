using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    /// <summary>
    /// Contains members available on every function.
    /// </summary>
    [MondPrototype("Function")]
    internal static partial class FunctionPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static FunctionPrototype()
        {
            ValueReadOnly = PrototypeObject.Build(ValuePrototype.Value);
        }

        private const string MustBeAFunction = "Function.{0}: must be called on a function";

        /// <summary>
        /// Returns the declared name of the function, or undefined when it has none.
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

using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Function")]
    internal static class FunctionPrototype
    {
        public static readonly MondValue Value;
        
        static FunctionPrototype()
        {
            Value = MondPrototypeBinder.Bind(typeof(FunctionPrototype));
            Value.Prototype = ValuePrototype.Value;

            Value.Lock();
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

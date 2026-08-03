using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Error", bareMethods: true)]
    internal static partial class ErrorModule
    {
        /// <summary>
        /// Throws a runtime error with the given message.
        /// </summary>
        [MondFunction]
        public static void Error(string message)
        {
            throw new MondRuntimeException(message);
        }

        /// <summary>
        /// Calls the function and returns an object holding either its result or the error it threw.
        /// </summary>
        [MondFunction]
        public static MondValue Try(MondState state, MondValue function, params Span<MondValue> arguments)
        {
            if (function.Type != MondValueType.Function)
                throw new MondRuntimeException("try: first argument must be a function");

            var obj = MondValue.Object();

            try
            {
                var result = state.Call(function, arguments);
                obj["result"] = result;
            }
            catch (Exception e)
            {
                obj["error"] = e.Message;
            }

            return obj;
        }
    }
}

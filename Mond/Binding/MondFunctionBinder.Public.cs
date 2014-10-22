using System;
using System.Reflection;

namespace Mond.Binding
{
    internal delegate object MondConstructor(MondState state, MondValue instance, params MondValue[] arguments);

    public static partial class MondFunctionBinder
    {
        /// <summary>
        /// Generates a MondFunction binding for the given method.
        /// </summary>
        public static MondFunction Bind(string moduleName, string methodName, MethodInfo method)
        {
            if (!method.IsStatic)
                throw new MondBindingException("Bind only supports static methods");

            return BindImpl<MondFunction, MondValue>(moduleName, methodName, method, false, (p, a, r) => BindFunctionCall(method, null, false, p, a, r));
        }

        /// <summary>
        /// Generates a MondInstanceFunction binding for the given method.
        /// </summary>
        internal static MondInstanceFunction BindInstance(string className, string methodName, MethodInfo method, Type type = null)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            if (type == null && !method.IsStatic)
                throw new MondBindingException("BindInstance requires a type for non-static methods");

            return BindImpl<MondInstanceFunction, MondValue>(className, methodName, method, true, (p, a, r) => BindFunctionCall(method, type, true, p, a, r));
        }

        /// <summary>
        /// Generates a MondConstructor binding for the given constructor.
        /// </summary>
        internal static MondConstructor BindConstructor(string className, ConstructorInfo constructor)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            return BindImpl<MondConstructor, object>(className, "#ctor", constructor, true, (p, a, r) => BindConstructorCall(constructor, a, r));
        }
    }
}

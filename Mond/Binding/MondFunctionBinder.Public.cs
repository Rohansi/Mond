using System;
using System.Reflection;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        /// <summary>
        /// Generates a MondFunction binding for the given method.
        /// </summary>
        public static MondFunction Bind(string moduleName, string methodName, MethodInfo method)
        {
            if (!method.IsStatic)
                throw new Exception("Bind only supports static methods");

            return BindImpl<MondFunction>(moduleName, methodName, method, false, (p, a, r) => BindFunctionCall(method, null, false, p, a, r));
        }

        /// <summary>
        /// Generates a MondInstanceFunction binding for the given method.
        /// </summary>
        internal static MondInstanceFunction BindInstance(string className, string methodName, MethodInfo method, Type type = null)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            if (type == null && !method.IsStatic)
                throw new Exception("BindInstance requires a type for non-static methods");

            return BindImpl<MondInstanceFunction>(className, methodName, method, true, (p, a, r) => BindFunctionCall(method, type, true, p, a, r));
        }

        /// <summary>
        /// Generates a MondFunction binding for the given constructor.
        /// </summary>
        internal static MondFunction BindConstructor(string className, ConstructorInfo constructor, MondValue prototype)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            return BindImpl<MondFunction>(className, "#ctor", constructor, false, (p, a, r) => BindConstructorCall(constructor, prototype, a, r));
        }
    }
}

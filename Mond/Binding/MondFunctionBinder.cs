using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    internal delegate object MondConstructor(MondState state, MondValue instance, params MondValue[] arguments);

    public static partial class MondFunctionBinder
    {
        /// <summary>
        /// Generates a MondFunction binding for the given function.
        /// </summary>
        public static MondFunction Bind(string moduleName, MethodInfo method, string nameOverride = null)
        {
            return BindStatic(moduleName, new[] { method }, nameOverride: nameOverride).Single().Item2;
        }

        /// <summary>
        /// Generates a MondFunction binding for the given functions.
        /// </summary>
        public static MondFunction Bind(string moduleName, ICollection<MethodInfo> methods, string nameOverride = null)
        {
            return BindStatic(moduleName, methods, nameOverride: nameOverride).Single().Item2;
        }

        /// <summary>
        /// Generates MondFunction bindings for the given methods.
        /// </summary>
        internal static IEnumerable<Tuple<string, MondFunction>> BindStatic(
            string moduleName,
            ICollection<MethodInfo> methods,
            MethodType methodType = MethodType.Normal,
            string nameOverride = null)
        {
            if (methods.Any(m => !m.IsStatic))
                throw new MondBindingException("BindStatic only supports static methods");

            var methodTables = BuildMethodTables(methods, methodType, nameOverride);

            foreach (var table in methodTables)
            {
#if !NO_EXPRESSIONS
                BindCallFactory callFactory = (e, m, p, a, r) => BindFunctionCall(e, m, null, false, p, a, r);
                yield return Tuple.Create(table.Name, BindImpl<MondFunction, MondValue>(moduleName, table, false, callFactory));
#else
                yield return Tuple.Create(table.Name, BindImpl(moduleName, table, nameOverride));
#endif
            }
        }

        /// <summary>
        /// Generates MondInstanceFunction bindings for the given methods.
        /// </summary>
        internal static IEnumerable<Tuple<string, MondInstanceFunction>> BindInstance(
            string className,
            ICollection<MethodInfo> methods,
            Type type = null,
            MethodType methodType = MethodType.Normal,
            string nameOverride = null)
        {
            if (className == null)
                throw new ArgumentNullException(nameof(className));

            if (type == null && methods.Any(m => !m.IsStatic))
                throw new MondBindingException("BindInstance requires a type for non-static methods");

            var methodTables = BuildMethodTables((IEnumerable<MethodBase>)methods, methodType, nameOverride);

            foreach (var table in methodTables)
            {
#if !NO_EXPRESSIONS
                BindCallFactory callFactory = (e, m, p, a, r) => BindFunctionCall(e, m, type, true, p, a, r);
                yield return Tuple.Create(table.Name, BindImpl<MondInstanceFunction, MondValue>(className, table, true, callFactory));
#else
                yield return Tuple.Create(table.Name, BindInstanceImpl(className, table, nameOverride, type == null));
#endif
            }
        }

        /// <summary>
        /// Generates a MondConstructor binding for the given constructors.
        /// </summary>
        internal static MondConstructor BindConstructor(string className, ICollection<ConstructorInfo> constructors)
        {
            if (className == null)
                throw new ArgumentNullException(nameof(className));

            var methodTable = BuildMethodTables((IEnumerable<MethodBase>)constructors, MethodType.Constructor).FirstOrDefault();

            if (methodTable == null || (methodTable.Methods.Count == 0 && methodTable.ParamsMethods.Count == 0))
                return null;

#if !NO_EXPRESSIONS
            BindCallFactory callFactory = (e, m, p, a, r) => BindConstructorCall(m, a, r);
            return BindImpl<MondConstructor, object>(className, methodTable, true, callFactory);
#else
            return BindConstructorImpl(className, methodTable);
#endif
        }
    }
}

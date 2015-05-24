using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mond.Binding
{
    public static class MondOperatorModuleBinder
    {
        private static Dictionary<Type, IEnumerable<Tuple<string, MondFunction>>> _cache = new Dictionary<Type, IEnumerable<Tuple<string, MondFunction>>>();

        public static void Bind<T>(MondState state)
        {
            Bind(typeof(T), state);
        }

        public static void Bind(Type type, MondState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            IEnumerable<Tuple<string, MondFunction>> bindings;
            lock (_cache) { bindings = _cache.ContainsKey( type ) ? _cache[type] : null; }

            if (bindings == null)
            {
                var operatorModuleAttr = type.Attribute<MondOperatorModuleAttribute>();

                if (operatorModuleAttr == null)
                    throw new MondBindingException(BindingError.TypeMissingAttribute, "MondOperatorModule");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                bindings = MondFunctionBinder.BindStatic(type.Name, methods, MondFunctionBinder.MethodType.Operator);

                lock (_cache) { _cache.Add(type, bindings); }
            }

            var opsObject = state["__ops"];
            foreach (var method in bindings)
                opsObject[method.Item1] = method.Item2;
        }
    }
}

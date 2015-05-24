using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mond.Binding
{
    public static class MondOperatorModuleBinder
    {
        private static HashSet<Type> _cache = new HashSet<Type>();

        public static void Bind<T>(MondState state)
        {
            Bind(typeof(T), state);
        }

        public static void Bind(Type type, MondState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            lock (_cache)
            {
                if (!_cache.Add(type))
                    return;
            }

            var operatorModuleAttr = type.Attribute<MondOperatorModuleAttribute>();

            if (operatorModuleAttr == null)
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondOperatorModule");

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var boundMethods = MondFunctionBinder.BindStatic(type.Name, methods);
            var opsObject = state["__ops"];

            foreach (var method in boundMethods)
                opsObject[method.Item1] = method.Item2;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    public static class MondOperatorModuleBinder
    {
        private class OperatorModuleBinding
        {
            public readonly Dictionary<string, MondFunction> Operators;

            public OperatorModuleBinding(Dictionary<string, MondFunction> operators)
            {
                Operators = operators;
            }
        }

        private static Dictionary<Type, OperatorModuleBinding> _cache = new Dictionary<Type, OperatorModuleBinding>();

        public static void Bind<T>(MondState state)
        {
            Bind(typeof(T), state);
        }

        public static void Bind(Type type, MondState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            OperatorModuleBinding binding;

            lock (_cache)
                _cache.TryGetValue(type, out binding);

            if (binding == null)
            {
                var operatorModuleAttr = type.Attribute<MondOperatorModuleAttribute>();

                if (operatorModuleAttr == null)
                    throw new MondBindingException(BindingError.TypeMissingAttribute, "MondOperatorModule");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                var boundMethods = MondFunctionBinder.BindStatic(type.Name, methods, MondFunctionBinder.MethodType.Operator);
                binding = new OperatorModuleBinding(boundMethods.ToDictionary(t => t.Item1, t => t.Item2));

                lock (_cache)
                    _cache[type] = binding;
            }

            var opsObject = state["__ops"];
            foreach (var method in binding.Operators)
                opsObject[method.Key] = method.Value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mond.VirtualMachine.Prototypes;

namespace Mond.Binding
{
    public static class MondClassBinder
    {
        private class ClassBinding
        {
            public MondConstructor Constructor { get; }
            public Dictionary<string, MondInstanceFunction> PrototypeFunctions { get; }

            public ClassBinding(MondConstructor constructor, Dictionary<string, MondInstanceFunction> prototypeFunctions)
            {
                Constructor = constructor;
                PrototypeFunctions = prototypeFunctions;
            }
        }

        private static Dictionary<Type, ClassBinding> _cache = new Dictionary<Type, ClassBinding>();

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function. The
        /// generated prototype will be locked.
        /// </summary>
        public static MondFunction Bind<T>(MondState state = null)
        {
            var ctor = Bind<T>(out var prototype, state);
            prototype.Lock();
            return ctor;
        }

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function and
        /// sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind<T>(out MondValue prototype, MondState state = null)
        {
            return Bind(typeof(T), out prototype, state);
        }

        /// <summary>
        /// Generates a class binding for the given type. Returns the constructor
        /// function and sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind(Type type, out MondValue prototype, MondState state = null)
        {
            var constructor = BindImpl(type, out var functions);
            var prototypeObj = CopyToObject(functions, state);

            prototype = prototypeObj;

            if (constructor == null)
                return null;

            return (_, arguments) =>
            {
                var instance = new MondValue(_);
                instance.Prototype = prototypeObj;
                instance.UserData = constructor(_, instance, arguments);
                return instance;
            };
        }

        private static MondValue CopyToObject(Dictionary<string, MondInstanceFunction> functions, MondState state)
        {
            var obj = new MondValue(state);
            obj.Prototype = MondValue.Null;

            foreach (var func in functions)
            {
                obj[func.Key] = new MondValue(func.Value);
            }

            obj.Prototype = ValuePrototype.Value;
            return obj;
        }

        private static MondConstructor BindImpl(Type type, out Dictionary<string, MondInstanceFunction> prototypeFunctions)
        {
            ClassBinding binding;

            lock (_cache)
            {
                if (_cache.TryGetValue(type, out binding))
                {
                    prototypeFunctions = binding.PrototypeFunctions;
                    return binding.Constructor;
                }
            }

            var classAttrib = type.Attribute<MondClassAttribute>();

            if (classAttrib == null)
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondClass");

            var className = classAttrib.Name ?? type.Name;

            var functions = new Dictionary<string, MondInstanceFunction>();

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in MondFunctionBinder.BindInstance(className, methods, type))
            {
                var name = method.Item1;

                if (functions.ContainsKey(name))
                    throw new MondBindingException(BindingError.DuplicateDefinition, name);

                functions[name] = method.Item2;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties.PropertyMethods())
            {
                var name = property.Item1;

                if (functions.ContainsKey(name))
                    throw new MondBindingException(BindingError.DuplicateDefinition, name);

                var propertyArray = new[] { property.Item2 };

                var propertyBinding = MondFunctionBinder.BindInstance(className, propertyArray, type, MondFunctionBinder.MethodType.Property, name)
                    .FirstOrDefault();

                if (propertyBinding != null)
                    functions[name] = propertyBinding.Item2;
            }

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = MondFunctionBinder.BindConstructor(className, constructors);

            binding = new ClassBinding(constructor, functions);

            lock (_cache)
                _cache[type] = binding;

            prototypeFunctions = functions;
            return constructor;
        }
    }
}

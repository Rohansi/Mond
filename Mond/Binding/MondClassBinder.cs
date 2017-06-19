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
            public string Name { get; }
            public MondConstructor Constructor { get; }
            public Dictionary<string, MondInstanceFunction> PrototypeFunctions { get; }

            public ClassBinding(string name, MondConstructor constructor, Dictionary<string, MondInstanceFunction> prototypeFunctions)
            {
                Name = name;
                Constructor = constructor;
                PrototypeFunctions = prototypeFunctions;
            }
        }

        private static readonly Dictionary<Type, ClassBinding> Cache = new Dictionary<Type, ClassBinding>();

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function. The
        /// generated prototype will be locked.
        /// </summary>
        public static MondFunction Bind<T>(MondState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var ctor = Bind<T>(state, out var prototype);
            prototype.Lock();
            return ctor;
        }

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function and
        /// sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind<T>(MondState state, out MondValue prototype)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return Bind(typeof(T), state, out prototype);
        }

        /// <summary>
        /// Generates a class binding for the given type. Returns the constructor
        /// function and sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind(Type type, MondState state, out MondValue prototype)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            ClassBinding binding;
            lock (Cache)
            {
                if (!Cache.TryGetValue(type, out binding))
                {
                    binding = BindImpl(type);
                    Cache[type] = binding;
                }
            }

            var prototypeObj = state.FindPrototype(binding.Name);
            if (prototypeObj == MondValue.Undefined)
            {
                prototypeObj = CopyToObject(binding.PrototypeFunctions, state);

                if (!state.TryAddPrototype(binding.Name, prototypeObj))
                    throw new MondBindingException(BindingError.DuplicatePrototype, binding.Name);
            }

            prototype = prototypeObj;

            var constructor = binding.Constructor;
            if (constructor == null)
                return null;

            return (_, arguments) =>
            {
                var instance = MondValue.Object(_);
                instance.Prototype = prototypeObj;
                instance.UserData = constructor(_, instance, arguments);
                return instance;
            };
        }

        private static MondValue CopyToObject(Dictionary<string, MondInstanceFunction> functions, MondState state)
        {
            var obj = MondValue.Object(state);
            obj.Prototype = MondValue.Null;

            foreach (var func in functions)
            {
                obj[func.Key] = MondValue.Function(func.Value);
            }

            obj.Prototype = ValuePrototype.Value;
            return obj;
        }

        private static ClassBinding BindImpl(Type type)
        {
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

            return new ClassBinding(className, constructor, functions);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using Mond.VirtualMachine.Prototypes;

namespace Mond.Binding
{
    public static class MondClassBinder
    {
        private class ClassBinding
        {
            public readonly MondConstructor Constructor;
            public readonly Dictionary<string, MondInstanceFunction> PrototypeFunctions;

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
            MondValue prototype;
            var ctor = Bind<T>(out prototype, state);
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
            Dictionary<string, MondInstanceFunction> functions;
            var constructor = BindImpl(type, out functions);
            var prototypeObj = CopyToObject(functions, state);

            prototype = prototypeObj;

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
            obj.Prototype = ValuePrototype.Value;

            foreach (var func in functions)
            {
                obj[func.Key] = new MondValue(func.Value);
            }

            return obj;
        }

        private static MondConstructor BindImpl(Type type, out Dictionary<string, MondInstanceFunction> prototypeFunctions)
        {
            ClassBinding binding;
            if (_cache.TryGetValue(type, out binding))
            {
                prototypeFunctions = binding.PrototypeFunctions;
                return binding.Constructor;
            }

            var classAttrib = type.Attribute<MondClassAttribute>();

            if (classAttrib == null)
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondClass");

            var className = classAttrib.Name ?? type.Name;

            MondConstructor constructor = null;
            var functions = new Dictionary<string, MondInstanceFunction>();

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;

                if (functions.ContainsKey(name))
                    throw new MondBindingException(BindingError.DuplicateDefinition, name);

                functions[name] = MondFunctionBinder.BindInstance(className, name, method, type);
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var functionAttrib = property.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? property.Name;

                var getMethod = property.GetGetMethod();
                var setMethod = property.GetSetMethod();

                if (getMethod != null && getMethod.IsPublic)
                {
                    var getMethodName = "get" + name;

                    if (functions.ContainsKey(getMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, getMethodName);

                    functions[getMethodName] = MondFunctionBinder.BindInstance(className, name, getMethod, type);
                }

                if (setMethod != null && setMethod.IsPublic)
                {
                    var setMethodName = "set" + name;

                    if (functions.ContainsKey(setMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, setMethodName);

                    functions[setMethodName] = MondFunctionBinder.BindInstance(className, name, setMethod, type);
                }
            }

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorAttrib = ctor.Attribute<MondConstructorAttribute>();

                if (constructorAttrib == null)
                    continue;

                if (constructor != null)
                    throw new MondBindingException(BindingError.TooManyConstructors);

                constructor = MondFunctionBinder.BindConstructor(className, ctor);
            }

            if (constructor == null)
                throw new MondBindingException(BindingError.NotEnoughConstructors);

            binding = new ClassBinding(constructor, functions);
            _cache.Add(type, binding);

            prototypeFunctions = functions;
            return constructor;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mond.Binding
{
    public static class MondClassBinder
    {
        private class ClassBinding
        {
            public readonly MondFunction Constructor;
            public readonly MondValue Prototype;

            public ClassBinding(MondFunction constructor, MondValue prototype)
            {
                Constructor = constructor;
                Prototype = prototype;
            }
        }

        private static Dictionary<Type, ClassBinding> _cache = new Dictionary<Type, ClassBinding>();

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function.
        /// </summary>
        public static MondFunction Bind<T>()
        {
            MondValue prototype;
            var ctor = Bind<T>(out prototype);
            prototype.Lock();
            return ctor;
        }

        /// <summary>
        /// Generate a class binding for T. Returns the constructor function and
        /// sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind<T>(out MondValue prototype)
        {
            return Bind(typeof(T), out prototype);
        }

        /// <summary>
        /// Generates a class binding for the given type. Returns the constructor
        /// function and sets prototype to the generated prototype.
        /// </summary>
        public static MondFunction Bind(Type type, out MondValue prototype)
        {
            ClassBinding binding;
            if (_cache.TryGetValue(type, out binding))
            {
                prototype = binding.Prototype;
                return binding.Constructor;
            }

            var classAttrib = type.Attribute<MondClassAttribute>();

            if (classAttrib == null)
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondClass");

            var className = classAttrib.Name ?? type.Name;

            var declarations = new HashSet<string>();
            MondFunction constructor = null;
            prototype = new MondValue(MondValueType.Object);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;

                if (!declarations.Add(name))
                    throw new MondBindingException(BindingError.DuplicateDefinition, name);

                prototype[name] = MondFunctionBinder.BindInstance(className, name, method, type);
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

                    if (!declarations.Add(getMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, getMethodName);

                    prototype[getMethodName] = MondFunctionBinder.BindInstance(className, name, getMethod, type);
                }

                if (setMethod != null && setMethod.IsPublic)
                {
                    var setMethodName = "set" + name;

                    if (!declarations.Add(setMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, setMethodName);

                    prototype[setMethodName] = MondFunctionBinder.BindInstance(className, name, setMethod, type);
                }
            }

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorAttrib = ctor.Attribute<MondConstructorAttribute>();

                if (constructorAttrib == null)
                    continue;

                if (constructor != null)
                    throw new MondBindingException(BindingError.TooManyConstructors);

                constructor = MondFunctionBinder.BindConstructor(className, ctor, prototype);
            }

            if (constructor == null)
                throw new MondBindingException(BindingError.NotEnoughConstructors);

            binding = new ClassBinding(constructor, prototype);
            _cache.Add(type, binding);

            return constructor;
        }
    }
}

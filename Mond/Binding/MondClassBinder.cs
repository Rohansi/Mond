using System;
using System.Collections.Generic;

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
                throw new Exception("Type does not have the MondClass attribute");

            var className = classAttrib.Name ?? type.Name;

            MondFunction constructor = null;
            prototype = new MondValue(MondValueType.Object);

            foreach (var method in type.GetMethods())
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;
                prototype[name] = MondFunctionBinder.BindInstance(className, name, method, type);
            }

            foreach (var property in type.GetProperties())
            {
                var functionAttrib = property.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? property.Name;

                var getMethod = property.GetGetMethod();
                var setMethod = property.GetSetMethod();

                if (getMethod != null && getMethod.IsPublic)
                    prototype["get" + name] = MondFunctionBinder.BindInstance(className, name, getMethod, type);

                if (setMethod != null && setMethod.IsPublic)
                    prototype["set" + name] = MondFunctionBinder.BindInstance(className, name, setMethod, type);
            }

            foreach (var ctor in type.GetConstructors())
            {
                var constructorAttrib = ctor.Attribute<MondClassConstructorAttribute>();

                if (constructorAttrib == null)
                    continue;

                if (constructor != null)
                    throw new Exception("Classes can not have multiple Mond constructors");

                constructor = MondFunctionBinder.BindConstructor(className, ctor, prototype);
            }

            if (constructor == null)
                throw new Exception("Classes must have one Mond constructor");

            binding = new ClassBinding(constructor, prototype);
            _cache.Add(type, binding);

            return constructor;
        }
    }
}

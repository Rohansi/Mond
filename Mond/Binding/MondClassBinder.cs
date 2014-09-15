using System;

namespace Mond.Binding
{
    public static class MondClassBinder
    {
        public static MondFunction Bind<T>()
        {
            MondValue prototype;
            var ctor = Bind<T>(out prototype);
            prototype.Lock();
            return ctor;
        }

        public static MondFunction Bind<T>(out MondValue prototype)
        {
            return Bind(typeof(T), out prototype);
        }

        public static MondFunction Bind(Type type, out MondValue prototype)
        {
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
                prototype[name] = MondFunctionBinder.BindInstance(className, name, type, method);
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
                    prototype["get" + name] = MondFunctionBinder.BindInstance(className, name, type, getMethod);

                if (setMethod != null && setMethod.IsPublic)
                    prototype["set" + name] = MondFunctionBinder.BindInstance(className, name, type, setMethod);
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

            return constructor;
        }
    }
}

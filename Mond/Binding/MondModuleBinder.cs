using System;
using System.Reflection;

namespace Mond.Binding
{
    public static class MondModuleBinder
    {
        public static MondValue Bind<T>()
        {
            return Bind(typeof(T));
        }

        public static MondValue Bind(Type type)
        {
            var moduleAttrib = type.Attribute<MondModuleAttribute>();

            if (moduleAttrib == null)
                throw new Exception("Type does not have the MondModule attribute");

            var moduleName = moduleAttrib.Name ?? type.Name;

            var result = new MondValue(MondValueType.Object);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;

                result[name] = MondFunctionBinder.Bind(moduleName, name, method);
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var functionAttrib = property.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? property.Name;

                var getMethod = property.GetGetMethod();
                var setMethod = property.GetSetMethod();

                if (getMethod != null && getMethod.IsPublic)
                    result["get" + name] = MondFunctionBinder.Bind(moduleName, name, getMethod);

                if (setMethod != null && setMethod.IsPublic)
                    result["set" + name] = MondFunctionBinder.Bind(moduleName, name, setMethod);
            }

            return result;
        }
    }
}

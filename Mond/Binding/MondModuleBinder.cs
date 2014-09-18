using System;
using System.Collections.Generic;
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
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondModule");

            var moduleName = moduleAttrib.Name ?? type.Name;

            var declarations = new HashSet<string>();
            var result = new MondValue(MondValueType.Object);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;

                if (!declarations.Add(name))
                    throw new MondBindingException(BindingError.DuplicateDefinition, name);

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
                {
                    var getMethodName = "get" + name;

                    if (!declarations.Add(getMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, getMethodName);

                    result[getMethodName] = MondFunctionBinder.Bind(moduleName, name, getMethod);
                }

                if (setMethod != null && setMethod.IsPublic)
                {
                    var setMethodName = "set" + name;

                    if (!declarations.Add(setMethodName))
                        throw new MondBindingException(BindingError.DuplicateDefinition, setMethodName);

                    result[setMethodName] = MondFunctionBinder.Bind(moduleName, name, setMethod);
                }
            }

            return result;
        }
    }
}

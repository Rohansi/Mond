using System;

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

            foreach (var method in type.GetMethods())
            {
                var functionAttrib = method.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? method.Name;

                result[name] = MondFunctionBinder.Bind(moduleName, name, method);
            }

            return result;
        }
    }
}

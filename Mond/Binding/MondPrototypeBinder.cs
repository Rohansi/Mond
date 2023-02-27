using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mond.Binding
{
    internal class MondPrototypeBinder
    {
        public static MondValue Bind(Type type)
        {
            var obj = MondValue.Object();

            foreach (var m in BindImpl(type))
            {
                obj.AsDictionary.Add(m.Item1, m.Item2);
            }

            return obj;
        }

        private static IEnumerable<Tuple<string, MondFunction>> BindImpl(Type type)
        {
            var moduleAttrib = type.Attribute<MondModuleAttribute>();

            if (moduleAttrib == null)
                throw new MondBindingException(BindingError.TypeMissingAttribute, "MondModule");

            var moduleName = moduleAttrib.Name ?? type.Name;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            return MondFunctionBinder.BindInstance(moduleName, methods);
        }
    }
}

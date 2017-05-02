using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    internal static class BindingUtil
    {
        public static T Attribute<T>(this Type type) where T : Attribute
        {
            return (T)type.GetTypeInfo().GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        public static T Attribute<T>(this MemberInfo member) where T : Attribute
        {
            return (T)member.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        public static T Attribute<T>(this ParameterInfo type) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        public static IEnumerable<Tuple<string, MethodInfo>> PropertyMethods(this IEnumerable<PropertyInfo> source)
        {
            foreach (var property in source)
            {
                var functionAttrib = property.Attribute<MondFunctionAttribute>();

                if (functionAttrib == null)
                    continue;

                var name = functionAttrib.Name ?? property.Name;

                var getMethod = property.GetMethod;
                if (getMethod != null && getMethod.IsPublic)
                {
                    yield return Tuple.Create("get" + name, getMethod);
                }

                var setMethod = property.SetMethod;
                if (setMethod != null && setMethod.IsPublic)
                {
                    yield return Tuple.Create("set" + name, setMethod);
                }
            }
        }
    }
}

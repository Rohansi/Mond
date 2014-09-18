using System;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    internal static class BindingUtil
    {
        public static T Attribute<T>(this MemberInfo type) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        public static T Attribute<T>(this ParameterInfo type) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }
    }
}

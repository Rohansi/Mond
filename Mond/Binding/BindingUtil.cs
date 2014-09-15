using System;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    static class BindingUtil
    {
        public static T Attribute<T>(this MemberInfo type) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }
    }
}

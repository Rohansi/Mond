using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondModuleAttribute : Attribute
    {
        public readonly string Name;

        public MondModuleAttribute(string name = null)
        {
            Name = name;
        }
    }
}

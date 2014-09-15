using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MondModuleAttribute : Attribute
    {
        public readonly string Name;

        public MondModuleAttribute(string name = null)
        {
            Name = name;
        }
    }
}

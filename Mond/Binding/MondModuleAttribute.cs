using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondModuleAttribute : Attribute
    {
        public string Name { get; }

        public MondModuleAttribute(string name = null)
        {
            Name = name;
        }
    }
}

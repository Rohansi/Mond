using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondClassAttribute : Attribute
    {
        public readonly string Name;

        public MondClassAttribute(string name = null)
        {
            Name = name;
        }
    }
}

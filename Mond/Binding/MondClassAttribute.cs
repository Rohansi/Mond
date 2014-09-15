using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MondClassAttribute : Attribute
    {
        public readonly string Name;

        public MondClassAttribute(string name = null)
        {
            Name = name;
        }
    }
}

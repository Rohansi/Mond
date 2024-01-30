using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class MondBindClassAttribute : Attribute
    {
        public string Name { get; }

        protected MondBindClassAttribute(string name)
        {
            Name = name;
        }
    }
}

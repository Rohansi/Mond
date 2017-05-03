using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class MondFunctionAttribute : Attribute
    {
        public string Name { get; }

        public MondFunctionAttribute(string name = null)
        {
            Name = name;
        }
    }
}

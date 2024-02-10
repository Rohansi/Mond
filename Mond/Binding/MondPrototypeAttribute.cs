using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class MondPrototypeAttribute : MondBindClassAttribute
    {
        public MondPrototypeAttribute(string name = null)
            : base(name)
        {
        }
    }
}

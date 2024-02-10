using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondModuleAttribute : MondBindClassAttribute
    {
        public bool BareMethods { get; }

        public MondModuleAttribute(string name = null, bool bareMethods = false)
            : base(name)
        {
            BareMethods = bareMethods;
        }
    }
}

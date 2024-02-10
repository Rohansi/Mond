using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondClassAttribute : MondBindClassAttribute
    {
        /// <summary>
        /// Allows the type to be used as a return value from other functions.
        /// </summary>
        public bool AllowReturn { get; set; }

        public MondClassAttribute(string name = null) 
            : base(name)
        {
            AllowReturn = true;
        }
    }
}

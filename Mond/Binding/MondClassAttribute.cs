using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MondClassAttribute : Attribute
    {
        public string Name { get; }

        /// <summary>
        /// Allows the type to be used as a return value from other functions.
        /// </summary>
        public bool AllowReturn { get; set; }

        public MondClassAttribute(string name = null)
        {
            Name = name;
            AllowReturn = true;
        }
    }
}

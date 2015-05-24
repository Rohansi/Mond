using System;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MondOperatorAttribute : Attribute
    {
        public string Operator { get; private set; }

        public MondOperatorAttribute(string @operator)
        {
            if (string.IsNullOrWhiteSpace(@operator))
                throw new ArgumentNullException("@operator");

            Operator = @operator;
        }
    }
}

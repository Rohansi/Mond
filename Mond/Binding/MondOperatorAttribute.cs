using System;
using Mond.Compiler;

namespace Mond.Binding
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MondOperatorAttribute : Attribute
    {
        public string Operator { get; private set; }

        public MondOperatorAttribute(string @operator)
        {
            if (!Lexer.IsOperatorToken(@operator))
                throw new ArgumentException($"`{@operator}` is not a valid operator name");

            if (Lexer.OperatorExists(@operator))
                throw new ArgumentException(CompilerError.CantOverrideBuiltInOperator, @operator);

            Operator = @operator;
        }
    }
}

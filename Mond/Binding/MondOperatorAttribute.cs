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
                throw new MondBindingException(BindingError.NameIsntValidOperator, @operator);

            if (Lexer.OperatorExists(@operator))
                throw new MondBindingException(CompilerError.CantOverrideBuiltInOperator, @operator);

            Operator = @operator;
        }
    }
}

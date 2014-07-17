using System;

namespace Mond.Compiler.Expressions
{
    class StringExpression : Expression, IConstantExpression
    {
        public string Value { get; private set; }

        public StringExpression(Token token, string value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("string: \"{0}\"", Value);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.Load(context.String(Value));
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return Value;
        }
    }
}

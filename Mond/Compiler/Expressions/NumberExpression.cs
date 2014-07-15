using System;

namespace Mond.Compiler.Expressions
{
    class NumberExpression : Expression, IConstantExpression
    {
        public double Value { get; private set; }

        public NumberExpression(Token token, double value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("number: {0}", Value);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            context.Load(context.Number(Value));
            return 1;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

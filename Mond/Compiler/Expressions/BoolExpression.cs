using System;

namespace Mond.Compiler.Expressions
{
    class BoolExpression : Expression, IConstantExpression
    {
        public bool Value { get; private set; }

        public BoolExpression(Token token, bool value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("bool: {0}", Value);
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            if (Value)
                context.LoadTrue();
            else
                context.LoadFalse();

            return 1;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

using System;

namespace Mond.Compiler.Expressions.Statements
{
    class ReturnExpression : Expression
    {
        public Expression Value { get; private set; }

        public ReturnExpression(Token token, Expression value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Return");

            if (Value != null)
                Value.Print(indent + 1);
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            if (Value != null)
                CompileCheck(context, Value, 1);
            else
                context.LoadUndefined();

            context.Return();
            return 0;
        }

        public override Expression Simplify()
        {
            Value = Value.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Value != null)
                Value.SetParent(this);
        }
    }
}

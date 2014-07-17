using System;

namespace Mond.Compiler.Expressions
{
    class FieldExpression : Expression, IStorableExpression
    {
        public readonly Expression Left;
        public string Name { get; private set; }

        public FieldExpression(Token token, Expression left)
            : base(token.FileName, token.Line)
        {
            Left = left;
            Name = token.Contents;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("Field {0}", Name);

            Left.Print(indent + 1);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;

            stack += Left.Compile(context);
            stack += context.LoadField(context.String(Name));

            return stack;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);
            stack += context.StoreField(context.String(Name));

            return stack;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
        }
    }
}

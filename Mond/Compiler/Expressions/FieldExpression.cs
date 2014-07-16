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

            CompileCheck(context, Left, 1);
            context.LoadField(context.String(Name));
            return 1;
        }

        public void CompileStore(FunctionContext context)
        {
            CompileCheck(context, Left, 1);
            context.StoreField(context.String(Name));
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

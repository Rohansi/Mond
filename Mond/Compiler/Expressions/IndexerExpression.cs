using System;

namespace Mond.Compiler.Expressions
{
    class IndexerExpression : Expression, IStorableExpression
    {
        public Expression Left { get; private set; }
        public Expression Index { get; private set; }

        public IndexerExpression(Token token, Expression left, Expression index)
            : base(token.FileName, token.Line)
        {
            Left = left;
            Index = index;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Indexer");

            Console.Write(indentStr);
            Console.WriteLine("-Left");

            Left.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-Index");

            Index.Print(indent + 2);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;

            stack += Left.Compile(context);
            stack += Index.Compile(context);
            stack += context.LoadArray();

            CheckStack(stack, 1);
            return stack;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);
            stack += Index.Compile(context);
            stack += context.StoreArray();

            return stack;
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();
            Index = Index.Simplify();

            return this;
        }
    }
}

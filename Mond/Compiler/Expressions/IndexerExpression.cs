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

            CompileCheck(context, Left, 1);
            CompileCheck(context, Index, 1);
            context.LoadArray();
            return 1;
        }

        public void CompileStore(FunctionContext context)
        {
            CompileCheck(context, Left, 1);
            CompileCheck(context, Index, 1);
            context.StoreArray();
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();
            Index = Index.Simplify();

            return this;
        }
    }
}

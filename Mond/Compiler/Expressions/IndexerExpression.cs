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

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Indexer");

            writer.WriteIndent();
            writer.WriteLine("-Left");

            writer.Indent += 2;
            Left.Print(writer);
            writer.Indent -= 2;

            writer.WriteIndent();
            writer.WriteLine("-Index");

            writer.Indent += 2;
            Index.Print(writer);
            writer.Indent -= 2;
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

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
            Index.SetParent(this);
        }
    }
}

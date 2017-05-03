namespace Mond.Compiler.Expressions
{
    class IndexerExpression : Expression, IStorableExpression
    {
        public Expression Left { get; private set; }
        public Expression Index { get; private set; }

        public override Token StartToken => Left.StartToken;

        public IndexerExpression(Token token, Expression left, Expression index)
            : base(token)
        {
            Left = left;
            Index = index;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);
            stack += Index.Compile(context);

            context.Position(Token); // debug info
            stack += context.LoadArray();

            CheckStack(stack, 1);
            return stack;
        }

        public int CompilePreLoadStore(FunctionContext context, int times)
        {
            var stack = 0;

            stack += Left.Compile(context);
            stack += Index.Compile(context);

            for (var i = 1; i < times; i++)
            {
                stack += context.Dup2();
            }

            return stack;
        }

        public int CompileLoad(FunctionContext context)
        {
            var stack = 0;

            context.Position(Token); // debug info
            stack += context.LoadArray();

            return stack;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;

            context.Position(Token); // debug info
            stack += context.StoreArray();

            return stack;
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();
            Index = Index.Simplify();
            
            if (Index is StringExpression indexStr)
            {
                var token = new Token(indexStr.Token, TokenType.String, indexStr.Value);
                return new FieldExpression(token, Left);
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
            Index.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

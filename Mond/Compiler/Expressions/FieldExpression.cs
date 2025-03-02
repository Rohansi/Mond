namespace Mond.Compiler.Expressions
{
    class FieldExpression : Expression, IStorableExpression
    {
        public Expression Left { get; private set; }
        public string Name { get; }

        public override Token StartToken => Left.StartToken;

        public FieldExpression(Token token, Expression left)
            : base(token)
        {
            Left = left;
            Name = token.Contents;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);

            context.Position(Token); // debug info
            stack += context.LoadField(context.String(Name));

            return stack;
        }

        public int CompilePreLoadStore(FunctionContext context, int times)
        {
            var stack = 0;

            stack += Left.Compile(context);

            for (var i = 1; i < times; i++)
            {
                stack += context.Dup();
            }

            return stack;
        }

        public int CompileLoad(FunctionContext context)
        {
            var stack = 0;

            context.Position(Token); // debug info
            stack += context.LoadField(context.String(Name));

            return stack;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;

            context.Position(Token); // debug info
            stack += context.StoreField(context.String(Name));

            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Left = Left.Simplify(context);
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

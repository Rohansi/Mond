namespace Mond.Compiler.Expressions
{
    class FieldExpression : Expression, IStorableExpression
    {
        public readonly Expression Left;
        public string Name { get; private set; }

        public FieldExpression(Token token, Expression left)
            : base(token.FileName, token.Line, token.Column)
        {
            Left = left;
            Name = token.Contents;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);

            context.Position(FileName, Line, Column); // debug info
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

            context.Position(FileName, Line, Column); // debug info
            stack += context.LoadField(context.String(Name));

            return stack;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;

            context.Position(FileName, Line, Column); // debug info
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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

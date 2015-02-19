namespace Mond.Compiler.Expressions
{
    class UnpackExpression : Expression
    {
        public Expression Right { get; private set; }

        public UnpackExpression(Token token, Expression right)
            : base(token)
        {
            Right = right;
        }

        public override int Compile(FunctionContext context)
        {
            var parentCall = Parent as CallExpression;
            if (parentCall == null || parentCall.Method == this)
                throw new MondCompilerException(this, CompilerError.UnpackMustBeInCall);

            return Right.Compile(context);
        }

        public override Expression Simplify()
        {
            Right = Right.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Right.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

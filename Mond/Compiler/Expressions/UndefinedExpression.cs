namespace Mond.Compiler.Expressions
{
    class UndefinedExpression : Expression, IConstantExpression
    {
        public UndefinedExpression(Token token)
            : base(token)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            return context.LoadUndefined();
        }

        public override Expression Simplify(SimplifyContext context)
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public MondValue GetValue()
        {
            return MondValue.Undefined;
        }
    }
}

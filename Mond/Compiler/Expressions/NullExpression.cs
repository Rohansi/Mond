namespace Mond.Compiler.Expressions
{
    class NullExpression : Expression, IConstantExpression
    {
        public NullExpression(Token token)
            : base(token)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            return context.LoadNull();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public MondValue GetValue()
        {
            return MondValue.Null;
        }
    }
}

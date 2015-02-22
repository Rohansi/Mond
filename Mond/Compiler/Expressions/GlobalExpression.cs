namespace Mond.Compiler.Expressions
{
    class GlobalExpression : Expression, IConstantExpression
    {
        public GlobalExpression(Token token)
            : base(token)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);
            return context.LoadGlobal();
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

namespace Mond.Compiler.Expressions
{
    class NumberExpression : Expression, IConstantExpression
    {
        public double Value { get; }

        public NumberExpression(Token token, double value)
            : base(token)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);
            return context.Load(context.Number(Value));
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
            return Value;
        }
    }
}

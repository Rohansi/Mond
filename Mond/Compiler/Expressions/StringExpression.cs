namespace Mond.Compiler.Expressions
{
    class StringExpression : Expression, IConstantExpression
    {
        public string Value { get; }

        public StringExpression(Token token, string value)
            : base(token)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);
            return context.Load(context.String(Value));
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
            return Value;
        }
    }
}

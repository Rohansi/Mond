namespace Mond.Compiler.Expressions
{
    class BoolExpression : Expression, IConstantExpression
    {
        public bool Value { get; }

        public BoolExpression(Token token, bool value)
            : base(token)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            if (Value)
                context.LoadTrue();
            else
                context.LoadFalse();

            return 1;
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

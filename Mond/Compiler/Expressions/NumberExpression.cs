namespace Mond.Compiler.Expressions
{
    class NumberExpression : Expression, IConstantExpression
    {
        public double Value { get; private set; }

        public NumberExpression(Token token, double value)
            : base(token.FileName, token.Line, token.Column)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(FileName, Line, Column);

            return context.Load(context.Number(Value));
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

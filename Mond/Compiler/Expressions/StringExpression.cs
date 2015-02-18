namespace Mond.Compiler.Expressions
{
    class StringExpression : Expression, IConstantExpression
    {
        public string Value { get; private set; }

        public StringExpression(Token token, string value)
            : base(token.FileName, token.Line, token.Column)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Line, Column);
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

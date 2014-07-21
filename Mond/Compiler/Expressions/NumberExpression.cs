namespace Mond.Compiler.Expressions
{
    class NumberExpression : Expression, IConstantExpression
    {
        public double Value { get; private set; }

        public NumberExpression(Token token, double value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("number: {0}", Value);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.Load(context.Number(Value));
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return Value;
        }
    }
}

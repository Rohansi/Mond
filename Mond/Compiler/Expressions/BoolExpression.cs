namespace Mond.Compiler.Expressions
{
    class BoolExpression : Expression, IConstantExpression
    {
        public bool Value { get; private set; }

        public BoolExpression(Token token, bool value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("bool: {0}", Value);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            if (Value)
                context.LoadTrue();
            else
                context.LoadFalse();

            return 1;
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

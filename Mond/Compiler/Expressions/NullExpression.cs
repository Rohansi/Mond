namespace Mond.Compiler.Expressions
{
    class NullExpression : Expression, IConstantExpression
    {
        public NullExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("null");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.LoadNull();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return MondValue.Null;
        }
    }
}

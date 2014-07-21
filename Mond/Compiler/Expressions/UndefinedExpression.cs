namespace Mond.Compiler.Expressions
{
    class UndefinedExpression : Expression, IConstantExpression
    {
        public UndefinedExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("undefined");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.LoadUndefined();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return MondValue.Undefined;
        }
    }
}

namespace Mond.Compiler.Expressions
{
    class EmptyExpression : Expression
    {
        public EmptyExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Blank");
        }

        public override int Compile(FunctionContext context)
        {
            return 0;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

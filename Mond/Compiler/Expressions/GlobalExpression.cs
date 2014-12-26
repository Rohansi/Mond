namespace Mond.Compiler.Expressions
{
    class GlobalExpression : Expression, IConstantExpression
    {
        public GlobalExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(FileName, Line, Column);

            return context.LoadGlobal();
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
            return MondValue.Null;
        }
    }
}

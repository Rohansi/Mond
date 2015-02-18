namespace Mond.Compiler.Expressions.Statements
{
    class DebuggerExpression : Expression, IStatementExpression
    {
        public DebuggerExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Line, Column);
            return context.Breakpoint();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

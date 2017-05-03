namespace Mond.Compiler.Expressions.Statements
{
    class DebuggerExpression : Expression, IStatementExpression
    {
        public bool HasChildren => false;

        public DebuggerExpression(Token token)
            : base(token)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);
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

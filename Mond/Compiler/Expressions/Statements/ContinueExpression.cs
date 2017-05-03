namespace Mond.Compiler.Expressions.Statements
{
    class ContinueExpression : Expression, IStatementExpression
    {
        public bool HasChildren => false;

        public ContinueExpression(Token token)
            : base(token)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var target = context.ContinueLabel();
            if (target == null)
                throw new MondCompilerException(this, CompilerError.UnresolvedJump);

            return context.Jump(target);
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

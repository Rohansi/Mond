namespace Mond.Compiler.Expressions.Statements
{
    class BreakExpression : Expression, IStatementExpression
    {
        public bool HasChildren => false;

        public BreakExpression(Token token)
            : base(token)
        {

        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var target = context.BreakLabel();
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

namespace Mond.Compiler.Expressions.Statements
{
    class BreakExpression : Expression, IStatementExpression
    {
        public BreakExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var target = context.BreakLabel();
            if (target == null)
                throw new MondCompilerException(FileName, Line, CompilerError.UnresolvedJump);

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

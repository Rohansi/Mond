namespace Mond.Compiler.Expressions.Statements
{
    class ContinueExpression : Expression, IStatementExpression
    {
        public ContinueExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(FileName, Line, Column);

            var target = context.ContinueLabel();
            if (target == null)
                throw new MondCompilerException(FileName, Line, Column, CompilerError.UnresolvedJump);

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

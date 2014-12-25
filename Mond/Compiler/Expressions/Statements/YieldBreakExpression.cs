namespace Mond.Compiler.Expressions.Statements
{
    class YieldBreakExpression : Expression, IStatementExpression
    {
        public YieldBreakExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(FileName, Line, Column);

            var sequenceContext = context.Root as SequenceBodyContext;
            if (sequenceContext == null)
                throw new MondCompilerException(FileName, Line, Column, CompilerError.YieldInFun);

            return context.Jump(sequenceContext.SequenceBody.EndLabel);
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

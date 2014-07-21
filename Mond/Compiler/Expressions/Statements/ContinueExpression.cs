namespace Mond.Compiler.Expressions.Statements
{
    class ContinueExpression : Expression, IStatementExpression
    {
        public ContinueExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Continue");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var target = context.ContinueLabel();
            if (target == null)
                throw new MondCompilerException(FileName, Line, CompilerError.UnresolvedJump);

            return context.Jump(target);
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

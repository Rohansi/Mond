using System;

namespace Mond.Compiler.Expressions.Statements
{
    class BreakExpression : Expression, IBlockStatementExpression
    {
        public BreakExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Break");
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
    }
}

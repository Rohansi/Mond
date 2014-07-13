using System;

namespace Mond.Compiler.Expressions
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

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            var target = context.BreakLabel();
            if (target == null)
                throw new MondCompilerException(FileName, Line, "Unresolved jump");

            context.Jump(target);
            return 0;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

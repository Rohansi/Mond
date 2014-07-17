using System;

namespace Mond.Compiler.Expressions.Statements
{
    class YieldBreakExpression : Expression, IBlockStatementExpression
    {
        public YieldBreakExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("YieldBreak");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var sequenceContext = context as SequenceBodyContext;
            if (sequenceContext == null)
                throw new MondCompilerException(FileName, Line, CompilerError.YieldInFun);

            return context.Jump(sequenceContext.SequenceBody.EndLabel);
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

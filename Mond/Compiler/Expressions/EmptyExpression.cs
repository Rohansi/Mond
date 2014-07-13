using System;

namespace Mond.Compiler.Expressions
{
    class EmptyExpression : Expression
    {
        public EmptyExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("Blank");
        }

        public override int Compile(CompilerContext context)
        {
            return 0;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}

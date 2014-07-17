using System;

namespace Mond.Compiler.Expressions
{
    class UndefinedExpression : Expression, IConstantExpression
    {
        public UndefinedExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Undefined");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.LoadUndefined();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return MondValue.Undefined;
        }
    }
}

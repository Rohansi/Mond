using System;

namespace Mond.Compiler.Expressions
{
    class NullExpression : Expression, IConstantExpression
    {
        public NullExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);
            Console.Write(indentStr);
            Console.WriteLine("null");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            context.LoadNull();
            return 1;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return MondValue.Null;
        }
    }
}

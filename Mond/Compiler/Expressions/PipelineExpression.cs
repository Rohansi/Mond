using System;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class PipelineExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public PipelineExpression(Token token, Expression left, Expression right)
            : base(token.FileName, token.Line)
        {
            Left = left;
            Right = right;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Pipeline");

            Left.Print(indent + 1);
            Right.Print(indent + 1);
        }

        public override int Compile(FunctionContext context)
        {
            var callExpression = Right as CallExpression;
            if (callExpression == null)
                throw new MondCompilerException(FileName, Line, CompilerError.PipelineNeedsCall);

            var token = new Token(callExpression.FileName, callExpression.Line, TokenType.LeftParen, null);
            var transformedArgs = Enumerable.Repeat(Left, 1).Concat(callExpression.Arguments).ToList();
            var transformedCall = new CallExpression(token, callExpression.Method, transformedArgs);

            return transformedCall.Compile(context);
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();
            Right = Right.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
            Right.SetParent(this);
        }
    }
}

using System;

namespace Mond.Compiler.Expressions
{
    class PostfixOperatorExpression : Expression
    {
        public TokenType Operation { get; private set; }
        public Expression Left { get; private set; }

        public PostfixOperatorExpression(Token token, Expression left)
            : base(token.FileName, token.Line)
        {
            Operation = token.Type;
            Left = left;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            var discardResult = Parent == null || Parent is BlockExpression;
            
            Console.Write(indentStr);
            Console.WriteLine("Postfix {0}" + (discardResult ? " - Result not used" : ""), Operation);

            Left.Print(indent + 1);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var storable = Left as IStorableExpression;
            if (storable == null)
                throw new MondCompilerException(FileName, Line, "The left-hand side of an assignment must be storable"); // TODO: better message

            var stack = 0;
            var needResult = !(Parent is BlockExpression);

            if (needResult)
                stack += Left.Compile(context);

            switch (Operation)
            {
                case TokenType.Increment:
                    stack += context.Load(context.Number(1));
                    stack += Left.Compile(context);
                    stack += context.BinaryOperation(TokenType.Add);
                    break;

                case TokenType.Decrement:
                    stack += context.Load(context.Number(1));
                    stack += Left.Compile(context);
                    stack += context.BinaryOperation(TokenType.Subtract);
                    break;

                default:
                    throw new NotSupportedException();
            }

            stack += storable.CompileStore(context);

            CheckStack(stack, needResult ? 1 : 0);
            return stack;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
        }
    }
}

using System;

namespace Mond.Compiler.Expressions
{
    class PostfixOperatorExpression : Expression
    {
        public TokenType Operation { get; private set; }
        public Expression Left { get; private set; }

        public PostfixOperatorExpression(Token token, Expression left)
            : base(token.FileName, token.Line, token.Column)
        {
            Operation = token.Type;
            Left = left;
        }

        public override int Compile(FunctionContext context)
        {
            var storable = Left as IStorableExpression;
            if (storable == null)
                throw new MondCompilerException(this, CompilerError.LeftSideMustBeStorable);

            var stack = 0;
            var needResult = !(Parent is IBlockExpression);

            if (needResult)
            {
                stack += Left.Compile(context);
                stack += context.Dup();
                stack += context.Load(context.Number(1));
                stack += context.Swap();
            }
            else
            {
                stack += Left.Compile(context);
                stack += context.Load(context.Number(1));
            }

            context.Position(FileName, Line, Column); // debug info

            switch (Operation)
            {
                case TokenType.Increment:
                    stack += context.BinaryOperation(TokenType.Add);
                    break;

                case TokenType.Decrement:
                    stack += context.BinaryOperation(TokenType.Subtract);
                    break;

                default:
                    throw new NotSupportedException();
            }

            stack += storable.CompilePreLoadStore(context, 1);
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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

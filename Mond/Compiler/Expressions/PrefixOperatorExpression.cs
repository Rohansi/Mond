using System;

namespace Mond.Compiler.Expressions
{
    class PrefixOperatorExpression : Expression
    {
        public TokenType Operation { get; }
        public Expression Right { get; private set; }

        public PrefixOperatorExpression(Token token, Expression right)
            : base(token)
        {
            Operation = token.Type;
            Right = right;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;
            var isAssignment = false;
            var needResult = !(Parent is IBlockExpression);
            
            switch (Operation)
            {
                case TokenType.Increment:
                    stack += Right.Compile(context);
                    stack += context.Load(context.Number(1));

                    context.Position(Token); // debug info
                    stack += context.BinaryOperation(TokenType.Add);
                    isAssignment = true;
                    break;

                case TokenType.Decrement:
                    stack += Right.Compile(context);
                    stack += context.Load(context.Number(1));

                    context.Position(Token); // debug info
                    stack += context.BinaryOperation(TokenType.Subtract);
                    isAssignment = true;
                    break;

                case TokenType.Subtract:
                case TokenType.Not:
                case TokenType.BitNot:
                    stack += Right.Compile(context);

                    context.Position(Token); // debug info
                    stack += context.UnaryOperation(Operation);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (isAssignment)
            {
                var storable = Right as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(this, CompilerError.LeftSideMustBeStorable);

                if (needResult)
                    stack += context.Dup();

                stack += storable.CompilePreLoadStore(context, 1);
                stack += storable.CompileStore(context);
            }

            CheckStack(stack, needResult ? 1 : 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Right = Right.Simplify();

            if (Operation == TokenType.Subtract)
            {
                if (Right is NumberExpression number)
                {
                    var token = new Token(Right.Token, TokenType.Number, null);
                    return new NumberExpression(token, -number.Value);
                }
            }

            if (Operation == TokenType.BitNot)
            {
                if (Right is NumberExpression number)
                {
                    var token = new Token(Right.Token, TokenType.Number, null);
                    return new NumberExpression(token, ~((int)number.Value));
                }
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Right.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

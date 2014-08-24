using System;

namespace Mond.Compiler.Expressions
{
    class PrefixOperatorExpression : Expression
    {
        public TokenType Operation { get; private set; }
        public Expression Right { get; private set; }

        public PrefixOperatorExpression(Token token, Expression right)
            : base(token.FileName, token.Line)
        {
            Operation = token.Type;
            Right = right;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Prefix {0}", Operation);

            writer.Indent++;
            Right.Print(writer);
            writer.Indent--;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var isAssignment = false;
            var needResult = !(Parent is IBlockExpression);
            
            switch (Operation)
            {
                case TokenType.Increment:
                    stack += context.Load(context.Number(1));
                    stack += Right.Compile(context);
                    stack += context.BinaryOperation(TokenType.Add);
                    isAssignment = true;
                    break;

                case TokenType.Decrement:
                    stack += context.Load(context.Number(1));
                    stack += Right.Compile(context);
                    stack += context.BinaryOperation(TokenType.Subtract);
                    isAssignment = true;
                    break;

                case TokenType.Subtract:
                case TokenType.Not:
                    stack += Right.Compile(context);
                    stack += context.UnaryOperation(Operation);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (isAssignment)
            {
                var storable = Right as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(FileName, Line, CompilerError.LeftSideMustBeStorable);

                if (needResult)
                    stack += context.Dup();

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
                var number = Right as NumberExpression;
                if (number != null)
                {
                    var token = new Token(Right.FileName, Right.Line, TokenType.Number, null);
                    return new NumberExpression(token, -number.Value);
                }
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Right.SetParent(this);
        }
    }
}

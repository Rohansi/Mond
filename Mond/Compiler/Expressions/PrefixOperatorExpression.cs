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

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Prefix {0}", Operation);

            Right.Print(indent + 1);
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            var isAssignment = false;
            var stack = 0;
            
            switch (Operation)
            {
                case TokenType.Increment:
                    context.Load(context.Number(1));
                    CompileCheck(context, Right, 1);
                    context.BinaryOperation(TokenType.Add);
                    isAssignment = true;
                    break;

                case TokenType.Decrement:
                    context.Load(context.Number(1));
                    CompileCheck(context, Right, 1);
                    context.BinaryOperation(TokenType.Subtract);
                    isAssignment = true;
                    break;

                case TokenType.Subtract:
                case TokenType.LogicalNot:
                    CompileCheck(context, Right, 1);
                    context.UnaryOperation(Operation);
                    stack++;
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (isAssignment)
            {
                var storable = Right as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(FileName, Line, "The left-hand side of an assignment must be storable"); // TODO: better message

                var needResult = !(Parent is BlockExpression);

                if (needResult)
                {
                    context.Dup();
                    stack++;
                }

                storable.CompileStore(context);
            }
            
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
                    Right = new NumberExpression(token, -number.Value);
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

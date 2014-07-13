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

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            var storable = Left as IStorableExpression;
            if (storable == null)
                throw new MondCompilerException(FileName, Line, "The left-hand side of an assignment must be storable"); // TODO: better message

            var needResult = !(Parent is BlockExpression);
            var stack = 0;

            if (needResult)
            {
                CompileCheck(context, Left, 1);
                stack++;
            }

            switch (Operation)
            {
                case TokenType.Increment:
                    context.Load(context.Number(1));
                    CompileCheck(context, Left, 1);
                    context.BinaryOperation(TokenType.Add);
                    break;

                case TokenType.Decrement:
                    context.Load(context.Number(1));
                    CompileCheck(context, Left, 1);
                    context.BinaryOperation(TokenType.Subtract);
                    break;

                default:
                    throw new NotSupportedException();
            }

            storable.CompileStore(context);
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

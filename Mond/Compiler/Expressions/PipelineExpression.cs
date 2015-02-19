using System.Linq;

namespace Mond.Compiler.Expressions
{
    class PipelineExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public override Token StartToken
        {
            get { return Left.StartToken; }
        }

        public PipelineExpression(Token token, Expression left, Expression right)
            : base(token)
        {
            Left = left;
            Right = right;
        }

        public override int Compile(FunctionContext context)
        {
            var callExpression = Right as CallExpression;
            if (callExpression == null)
                throw new MondCompilerException(this, CompilerError.PipelineNeedsCall);

            var token = new Token(callExpression.Token, TokenType.LeftParen, null);
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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

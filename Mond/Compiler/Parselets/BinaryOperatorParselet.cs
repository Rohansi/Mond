using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class BinaryOperatorParselet : IInfixParselet
    {
        private readonly int _precedence;
        private readonly bool _isRight;

        public int Precedence => _precedence;

        public BinaryOperatorParselet(int precedence, bool isRight)
        {
            _precedence = precedence;
            _isRight = isRight;
        }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var right = parser.ParseExpression(Precedence - (_isRight ? 1 : 0));

            if (token.Type == TokenType.UserDefinedOperator)
                return new UserDefinedBinaryOperatorExpression(token, left, right);

            return new BinaryOperatorExpression(token, left, right);
        }
    }
}

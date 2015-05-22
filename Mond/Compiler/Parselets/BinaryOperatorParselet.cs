using System.Text;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class BinaryOperatorParselet : IInfixParselet
    {
        private readonly int _precedence;
        private readonly bool _isRight;

        public int Precedence { get { return _precedence; } }

        public BinaryOperatorParselet(int precedence, bool isRight)
        {
            _precedence = precedence;
            _isRight = isRight;
        }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            Expression right;

            if (token.SubType == TokenSubType.Operator)
            {
                var @operator = new StringBuilder(token.Contents);

                while (parser.Match(TokenSubType.Operator))
                    @operator.Append(parser.Take().Contents);

                token = new Token(token, TokenType.UserDefinedOperator, @operator.ToString(), TokenSubType.Operator);
                right = parser.ParseExpression((int)PrecedenceValue.Relational);
                return new UserDefinedBinaryOperatorExpression(token, left, right, token.Contents);
            }

            right = parser.ParseExpression(Precedence - (_isRight ? 1 : 0));
            return new BinaryOperatorExpression(token, left, right);
        }
    }
}

using System.Text;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class PrefixOperatorParselet : IPrefixParselet
    {
        private readonly int _precedence;

        public PrefixOperatorParselet(int precedence)
        {
            _precedence = precedence;
        }

        public Expression Parse(Parser parser, Token token)
        {
            Expression right;

            if (token.SubType == TokenSubType.Operator)
            {
                var @operator = new StringBuilder(token.Contents);

                while (parser.Match(TokenSubType.Operator))
                    @operator.Append(parser.Take().Contents);

                token = new Token(token, TokenType.UserDefinedOperator, @operator.ToString());
                right = parser.ParseExpression((int)PrecedenceValue.Prefix);
                return new UserDefinedUnaryOperator(token, right, token.Contents);
            }

            right = parser.ParseExpression(_precedence);
            return new PrefixOperatorExpression(token, right);
        }
    }
}

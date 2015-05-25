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
            var right = parser.ParseExpression(_precedence);

            if (token.Type == TokenType.UserDefinedOperator)
                return new UserDefinedUnaryOperator(token, right);

            return new PrefixOperatorExpression(token, right);
        }
    }
}

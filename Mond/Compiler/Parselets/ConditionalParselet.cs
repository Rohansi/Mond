using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class ConditionalParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Ternary; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var trueExpr = parser.ParseExpession();
            parser.Take(TokenType.Colon);
            var falseExpr = parser.ParseExpession();

            return new TernaryExpression(token, left, trueExpr, falseExpr);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class InfinityParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            return new NumberExpression(token, double.PositiveInfinity);
        }
    }
}

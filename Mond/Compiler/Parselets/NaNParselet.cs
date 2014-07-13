using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class NaNParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            return new NumberExpression(token, double.NaN);
        }
    }
}

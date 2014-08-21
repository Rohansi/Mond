using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class GlobalParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            return new GlobalExpression(token);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class NullParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            return new NullExpression(token);
        }
    }
}

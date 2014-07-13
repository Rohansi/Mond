using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class StringParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            return new StringExpression(token, token.Contents);
        }
    }
}

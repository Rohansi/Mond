using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class GroupParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var expression = parser.ParseExpession();
            parser.Take(TokenType.RightParen);
            return expression;
        }
    }
}

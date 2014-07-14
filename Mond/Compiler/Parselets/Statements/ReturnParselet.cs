using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class ReturnParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            Expression value = null;
            if (!parser.Match(TokenType.Semicolon))
                value = parser.ParseExpession();

            return new ReturnExpression(token, value);
        }
    }
}

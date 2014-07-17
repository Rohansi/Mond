using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class YieldParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            if (parser.MatchAndTake(TokenType.Break))
                return new YieldBreakExpression(token);

            var value = parser.ParseExpession();
            return new YieldExpression(token, value);
        }
    }
}

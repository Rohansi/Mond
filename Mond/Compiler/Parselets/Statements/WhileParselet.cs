using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class WhileParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            parser.Take(TokenType.LeftParen);

            var condition = parser.ParseExpression();

            parser.Take(TokenType.RightParen);

            var block = new ScopeExpression(parser.ParseBlock());
            return new WhileExpression(token, condition, block);
        }
    }
}

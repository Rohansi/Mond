using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class WhileParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return false; } }

        public Expression Parse(Parser parser, Token token)
        {
            parser.Take(TokenType.LeftParen);

            var condition = parser.ParseExpession();

            parser.Take(TokenType.RightParen);

            var block = new ScopeExpression(parser.ParseBlock());
            return new WhileExpression(token, condition, block);
        }
    }
}

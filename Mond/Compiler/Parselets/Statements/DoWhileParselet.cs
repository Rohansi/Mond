using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class DoWhileParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return true; } }

        public Expression Parse(Parser parser, Token token)
        {
            var block = new ScopeExpression(parser.ParseBlock());

            parser.Take(TokenType.While);
            parser.Take(TokenType.LeftParen);

            var condition = parser.ParseExpession();

            parser.Take(TokenType.RightParen);

            return new DoWhileExpression(token, block, condition);
        }
    }
}

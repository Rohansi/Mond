using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class ReturnParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return true; } }

        public Expression Parse(Parser parser, Token token)
        {
            Expression value = null;
            if (!parser.Match(TokenType.Semicolon))
                value = parser.ParseExpession();

            return new ReturnExpression(token, value);
        }
    }
}

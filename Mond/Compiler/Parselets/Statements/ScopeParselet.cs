using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets.Statements
{
    class ScopeParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            var statements = new List<Expression>();

            while (!parser.Match(TokenType.RightBrace))
            {
                statements.Add(parser.ParseStatement());
            }

            parser.Take(TokenType.RightBrace);
            return new ScopeExpression(statements);
        }
    }
}

using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets.Statements
{
    class ScopeParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return false; } }

        public Expression Parse(Parser parser, Token token)
        {
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

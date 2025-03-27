using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    internal class DeclareGlobalsParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            var names = new List<string>();
            do
            {
                var identifier = parser.Take(TokenType.Identifier);
                names.Add(identifier.Contents);
            } while (parser.MatchAndTake(TokenType.Comma));

            return new DeclareGlobalsExpression(token, names);
        }
    }
}

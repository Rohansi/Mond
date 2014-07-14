using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class VarParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            var declarations = new List<VarExpression.Declaration>();

            do
            {
                var identifier = parser.Take(TokenType.Identifier);
                Expression initializer = null;

                if (parser.MatchAndTake(TokenType.Assign))
                {
                    initializer = parser.ParseExpession();
                }

                var declaration = new VarExpression.Declaration(identifier.Contents, initializer);
                declarations.Add(declaration);
            } while (parser.MatchAndTake(TokenType.Comma));

            return new VarExpression(token, declarations);
        }
    }
}

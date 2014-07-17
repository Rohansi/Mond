using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class ForeachParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            parser.Take(TokenType.LeftParen);
            parser.Take(TokenType.Var);

            var identifier = parser.Take(TokenType.Identifier).Contents;

            parser.Take(TokenType.Colon);

            var expression = parser.ParseExpession();

            parser.Take(TokenType.RightParen);

            var block = parser.ParseBlock();

            return new ForeachExpression(token, identifier, expression, block);
        }
    }
}

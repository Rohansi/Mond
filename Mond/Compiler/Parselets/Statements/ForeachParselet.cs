using System.Linq;
using System.Collections.Generic;
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
            var varToken = parser.Take(TokenType.Var);
            Token inToken;
            Expression declaration;
            Expression expression;
            BlockExpression block;

            if (parser.MatchAndTake(TokenType.LeftBrace))
            {
                var fields = VarParselet.ParseObjectDestructuring(parser);
                inToken = parser.Take(TokenType.In);

                expression = parser.ParseExpression();
                declaration = new DestructuredObjectExpression(varToken, fields, null, false);

                parser.Take(TokenType.RightParen);
                block = parser.ParseBlock();

                return new ForeachExpression(token, inToken, "input", expression, block, declaration);
            }

            if (parser.MatchAndTake(TokenType.LeftSquare))
            {
                var indecies = VarParselet.ParseArrayDestructuring(parser);
                inToken = parser.Take(TokenType.In);

                expression = parser.ParseExpression();
                declaration = new DestructuredArrayExpression(varToken, indecies, null, false);

                parser.Take(TokenType.RightParen);
                block = parser.ParseBlock();

                return new ForeachExpression(token, inToken, "input", expression, block, declaration);
            }

            var identifier = parser.Take(TokenType.Identifier).Contents;

            inToken = parser.Take(TokenType.In);
            expression = parser.ParseExpression();

            parser.Take(TokenType.RightParen);

            block = parser.ParseBlock();

            return new ForeachExpression(token, inToken, identifier, expression, block);
        }
    }
}

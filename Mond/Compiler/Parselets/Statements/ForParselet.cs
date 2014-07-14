using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class ForParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            parser.Take(TokenType.LeftParen);

            Expression initializer = null;
            if (!parser.Match(TokenType.Semicolon))
                initializer = parser.ParseStatement(false);

            if (initializer is IBlockStatementExpression)
                throw new MondCompilerException(token.FileName, token.Line, "For loop initializer can not be block statement");

            parser.Take(TokenType.Semicolon);

            Expression condition = null;
            if (!parser.Match(TokenType.Semicolon))
                condition = parser.ParseExpession();

            parser.Take(TokenType.Semicolon);

            BlockExpression increment = null;
            if (!parser.Match(TokenType.RightParen))
            {
                var statements = new List<Expression>();

                do
                {
                    statements.Add(parser.ParseStatement(false));

                    if (!parser.Match(TokenType.Comma))
                        break;

                    parser.Take(TokenType.Comma);
                } while (true);

                increment = new BlockExpression(token, statements);
            }

            parser.Take(TokenType.RightParen);

            var block = new ScopeExpression(parser.ParseBlock());

            return new ForExpression(token, initializer, condition, increment, block);
        }
    }
}

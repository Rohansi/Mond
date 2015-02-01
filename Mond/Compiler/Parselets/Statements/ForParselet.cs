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

            BlockExpression initializer = null;
            if (!parser.Match(TokenType.Semicolon))
            {
                var initializerExpr = parser.ParseStatement(false);

                if (initializerExpr is IStatementExpression && !(initializerExpr is VarExpression))
                    throw new MondCompilerException(token, CompilerError.BadForLoopInitializer);

                initializer = new BlockExpression(new List<Expression>()
                {
                    initializerExpr
                });
            }

            parser.Take(TokenType.Semicolon);

            Expression condition = null;
            if (!parser.Match(TokenType.Semicolon))
                condition = parser.ParseExpression();

            parser.Take(TokenType.Semicolon);

            BlockExpression increment = null;
            if (!parser.Match(TokenType.RightParen))
            {
                var statements = new List<Expression>();

                do
                {
                    statements.Add(parser.ParseExpression());

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

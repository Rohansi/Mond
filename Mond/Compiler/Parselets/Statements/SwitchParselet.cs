using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class SwitchParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            parser.Take(TokenType.LeftParen);

            var expression = parser.ParseExpression();

            parser.Take(TokenType.RightParen);
            parser.Take(TokenType.LeftBrace);

            var hasDefault = false;
            var branches = new List<SwitchExpression.Branch>();

            while (!parser.Match(TokenType.RightBrace))
            {
                var conditions = new List<Expression>();

                while (true)
                {
                    if (parser.MatchAndTake(TokenType.Case))
                    {
                        var condition = parser.ParseExpression();
                        conditions.Add(condition);

                        parser.Take(TokenType.Colon);
                        continue;
                    }

                    if (!parser.Match(TokenType.Default))
                        break;

                    var defaultToken = parser.Take(TokenType.Default);

                    if (hasDefault)
                        throw new MondCompilerException(defaultToken, CompilerError.DuplicateDefault);

                    conditions.Add(null); // special default condition
                    hasDefault = true;

                    parser.Take(TokenType.Colon);
                }

                if (conditions.Count > 0)
                {
                    var block = ParseBlock(parser);
                    var branch = new SwitchExpression.Branch(conditions, block);
                    branches.Add(branch);
                    continue;
                }

                var errorToken = parser.Peek();
                throw new MondCompilerException(errorToken, CompilerError.ExpectedButFound2, TokenType.Case, TokenType.Default, errorToken);
            }

            parser.Take(TokenType.RightBrace);

            return new SwitchExpression(token, expression, branches);
        }

        private static BlockExpression ParseBlock(Parser parser)
        {
            var statements = new List<Expression>();
            
            while (!parser.Match(TokenType.Case) &&
                   !parser.Match(TokenType.Default) &&
                   !parser.Match(TokenType.RightBrace))
            {
                statements.Add(parser.ParseStatement());
            }

            return new ScopeExpression(statements);
        }
    }
}

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

            var expression = parser.ParseExpession();

            parser.Take(TokenType.RightParen);
            parser.Take(TokenType.LeftBrace);

            var branches = new List<SwitchExpression.Branch>();
            BlockExpression defaultBlock = null;

            while (!parser.Match(TokenType.RightBrace))
            {
                var conditions = new List<Expression>();

                while (parser.MatchAndTake(TokenType.Case))
                {
                    var condition = parser.ParseExpession();
                    conditions.Add(condition);

                    parser.Take(TokenType.Colon);
                }

                if (conditions.Count > 0)
                {
                    var block = ParseBlock(parser);
                    var branch = new SwitchExpression.Branch(conditions, block);
                    branches.Add(branch);
                    continue;
                }
                
                if (parser.MatchAndTake(TokenType.Default))
                {
                    parser.Take(TokenType.Colon);

                    var block = ParseBlock(parser);
                    defaultBlock = block;
                    break;
                }

                var errorToken = parser.Peek();
                throw new MondCompilerException(errorToken.FileName, errorToken.Line, CompilerError.ExpectedButFound2, TokenType.Case, TokenType.Default, errorToken);
            }

            parser.Take(TokenType.RightBrace);

            return new SwitchExpression(token, expression, branches, defaultBlock);
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

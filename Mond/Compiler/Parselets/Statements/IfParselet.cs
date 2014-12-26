using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class IfParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            var first = true;
            var branches = new List<IfExpression.Branch>();
            IfExpression.Branch elseBranch = null;

            do
            {
                var isDefaultBlock = !first && !parser.MatchAndTake(TokenType.If);
                first = false;

                Expression condition = null;
                if (!isDefaultBlock)
                {
                    parser.Take(TokenType.LeftParen);

                    condition = parser.ParseExpression();

                    parser.Take(TokenType.RightParen);
                }

                var block = new ScopeExpression(parser.ParseBlock());
                var branch = new IfExpression.Branch(condition, block);

                if (isDefaultBlock)
                    elseBranch = branch;
                else
                    branches.Add(branch);

                if (isDefaultBlock)
                    break;
            } while (parser.MatchAndTake(TokenType.Else));

            return new IfExpression(token, branches, elseBranch);
        }
    }
}

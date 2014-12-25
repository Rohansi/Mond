using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class CallParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Postfix; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var args = new List<Expression>();

            if (!parser.MatchAndTake(TokenType.RightParen))
            {
                do
                {
                    args.Add(parser.ParseExpression());
                } while (parser.MatchAndTake(TokenType.Comma));

                parser.Take(TokenType.RightParen);
            }

            return new CallExpression(token, left, args);
        }
    }
}

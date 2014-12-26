using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class IndexerParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Postfix; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            if (parser.Match(TokenType.Colon))
                return ParseSlice(parser, left, token);

            var index = parser.ParseExpression();

            if (parser.Match(TokenType.Colon))
                return ParseSlice(parser, left, token, index);

            parser.Take(TokenType.RightSquare);
            return new IndexerExpression(token, left, index);
        }

        private static Expression ParseSlice(Parser parser, Expression left, Token token, Expression start = null)
        {
            parser.Take(TokenType.Colon);

            if (parser.MatchAndTake(TokenType.RightSquare))
                return new SliceExpression(token, left, start, null, null);

            Expression end = null;

            if (!parser.Match(TokenType.Colon))
            {
                end = parser.ParseExpression();

                if (parser.MatchAndTake(TokenType.RightSquare))
                    return new SliceExpression(token, left, start, end, null);
            }

            parser.Take(TokenType.Colon);

            var step = parser.ParseExpression();

            parser.Take(TokenType.RightSquare);

            return new SliceExpression(token, left, start, end, step);
        }
    }
}

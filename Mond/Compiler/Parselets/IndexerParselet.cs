using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class IndexerParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Postfix; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var index = parser.ParseExpession();
            parser.Take(TokenType.RightSquare);
            return new IndexerExpression(token, left, index);
        }
    }
}

using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class ArrayParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var values = new List<Expression>();

            while (!parser.Match(TokenType.RightSquare))
            {
                var value = parser.ParseExpession();
                values.Add(value);

                if (!parser.Match(TokenType.Comma))
                    break;

                parser.Take(TokenType.Comma);
            }

            parser.Take(TokenType.RightSquare);

            return new ArrayExpression(token, values);
        }
    }
}

using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class ObjectParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var values = new List<KeyValuePair<string, Expression>>();

            while (!parser.Match(TokenType.RightBrace))
            {
                var identifier = parser.Take(TokenType.Identifier);

                Expression value;

                if (parser.Match(TokenType.Comma) || parser.Match(TokenType.RightBrace))
                {
                    value = new IdentifierExpression(identifier);
                }
                else
                {
                    parser.Take(TokenType.Colon);
                    value = parser.ParseExpession();
                }

                values.Add(new KeyValuePair<string, Expression>(identifier.Contents, value));

                if (!parser.Match(TokenType.Comma))
                    break;

                parser.Take(TokenType.Comma);
            }

            parser.Take(TokenType.RightBrace);

            return new ObjectExpression(token, values);
        }
    }
}

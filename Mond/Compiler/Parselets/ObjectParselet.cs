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
                string key;
                Expression value = null;

                if (parser.Match(TokenType.Identifier))
                {
                    var identifier = parser.Take(TokenType.Identifier);
                    key = identifier.Contents;

                    if (parser.Match(TokenType.Comma) || parser.Match(TokenType.RightBrace))
                    {
                        value = new IdentifierExpression(identifier);
                    }
                }
                else if (parser.Match(TokenType.String))
                {
                    key = parser.Take(TokenType.String).Contents;
                }
                else
                {
                    var err = parser.Take();
                    throw new MondCompilerException(err.FileName, err.Line, CompilerError.ExpectedButFound2, TokenType.Identifier, TokenType.String, err.Type);
                }

                if (value == null)
                {
                    parser.Take(TokenType.Colon);
                    value = parser.ParseExpession();
                }

                values.Add(new KeyValuePair<string, Expression>(key, value));

                if (!parser.Match(TokenType.Comma))
                    break;

                parser.Take(TokenType.Comma);
            }

            parser.Take(TokenType.RightBrace);

            return new ObjectExpression(token, values);
        }
    }
}

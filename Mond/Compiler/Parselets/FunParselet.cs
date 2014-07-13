using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets
{
    class FunParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var arguments = new List<string>();

            if (parser.Match(TokenType.LeftParen))
            {
                parser.Take(TokenType.LeftParen);

                if (!parser.Match(TokenType.RightParen))
                {
                    while (true)
                    {
                        var identifier = parser.Take(TokenType.Identifier);
                        arguments.Add(identifier.Contents);

                        if (parser.Match(TokenType.RightParen))
                            break;

                        parser.Take(TokenType.Comma);
                    }
                }

                parser.Take(TokenType.RightParen);
            }
            else
            {
                var identifier = parser.Take(TokenType.Identifier);
                arguments.Add(identifier.Contents);
            }

            parser.Take(TokenType.Pointy);

            BlockExpression body;

            if (parser.Match(TokenType.LeftBrace))
            {
                body = parser.ParseBlock(false);
            }
            else
            {
                body = new BlockExpression(new List<Expression>
                {
                    new ReturnExpression(token, parser.ParseExpession())
                });
            }

            return new FunctionExpression(token, null, arguments, body);
        }
    }
}

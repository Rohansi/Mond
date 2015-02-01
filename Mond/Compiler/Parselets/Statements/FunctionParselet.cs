﻿using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class FunctionParselet : IStatementParselet, IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;

            string name = null;
            var arguments = new List<string>();
            string otherArgs = null;
            BlockExpression body;

            // optional name
            if (parser.Match(TokenType.Identifier))
            {
                name = parser.Take(TokenType.Identifier).Contents;
            }

            // parse argument list
            parser.Take(TokenType.LeftParen);

            if (!parser.Match(TokenType.RightParen))
            {
                while (true)
                {
                    if (parser.MatchAndTake(TokenType.Ellipsis))
                    {
                        otherArgs = parser.Take(TokenType.Identifier).Contents;
                        break;
                    }

                    var identifier = parser.Take(TokenType.Identifier);
                    arguments.Add(identifier.Contents);

                    if (parser.Match(TokenType.RightParen))
                        break;

                    parser.Take(TokenType.Comma);
                }
            }

            parser.Take(TokenType.RightParen);

            // parse body
            if (parser.MatchAndTake(TokenType.Pointy))
            {
                body = new BlockExpression(new List<Expression>
                {
                    new ReturnExpression(token, parser.ParseExpression())
                });

                trailingSemicolon = true;
            }
            else
            {
                body = parser.ParseBlock(false);
            }

            return new FunctionExpression(token, name, arguments, otherArgs, body);
        }

        public Expression Parse(Parser parser, Token token)
        {
            bool hasTrailingSemicolon;
            return Parse(parser, token, out hasTrailingSemicolon);
        }

        public static BlockExpression ParseLambdaExpressionBody(Parser parser, Token token)
        {
            if (parser.Match(TokenType.LeftBrace))
                return parser.ParseBlock();

            return new BlockExpression(new List<Expression>
            {
                new ReturnExpression(token, parser.ParseExpression())
            });
        }
    }
}

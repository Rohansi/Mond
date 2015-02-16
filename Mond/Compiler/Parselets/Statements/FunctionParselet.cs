using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class FunctionParselet : IStatementParselet, IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            string name;
            List<string> arguments;
            string otherArgs;
            ScopeExpression body;

            ParseFunction(parser, token, true, out trailingSemicolon, out name, out arguments, out otherArgs, out body);

            return new FunctionExpression(token, name, arguments, otherArgs, body);
        }

        public Expression Parse(Parser parser, Token token)
        {
            string name;
            List<string> arguments;
            string otherArgs;
            ScopeExpression body;
            bool trailingSemicolon;

            ParseFunction(parser, token, false, out trailingSemicolon, out name, out arguments, out otherArgs, out body);

            return new FunctionExpression(token, name, arguments, otherArgs, body);
        }

        public static void ParseFunction(
            Parser parser,
            Token token,
            bool isStatement,
            out bool trailingSemicolon,
            out string name,
            out List<string> arguments,
            out string otherArgs,
            out ScopeExpression body)
        {
            trailingSemicolon = false;

            name = null;
            arguments = new List<string>();
            otherArgs = null;

            // only statements can be named
            if (isStatement)
                name = parser.Take(TokenType.Identifier).Contents;

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
                body = new ScopeExpression(new List<Expression>
                {
                    new ReturnExpression(token, parser.ParseExpression())
                });

                trailingSemicolon = true;
            }
            else
            {
                body = new ScopeExpression(parser.ParseBlock(false));
            }
        }

        public static ScopeExpression ParseLambdaExpressionBody(Parser parser, Token token)
        {
            if (parser.Match(TokenType.LeftBrace))
                return new ScopeExpression(parser.ParseBlock());

            return new ScopeExpression(new List<Expression>
            {
                new ReturnExpression(token, parser.ParseExpression())
            });
        }
    }
}

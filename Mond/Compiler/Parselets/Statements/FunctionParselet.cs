using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class FunctionParselet : IStatementParselet, IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            ParseFunction(parser, token, true, out trailingSemicolon,
                out var name,
                out var arguments,
                out var otherArgs,
                out var body);

            return new FunctionExpression(token, name, arguments, otherArgs, body);
        }

        public Expression Parse(Parser parser, Token token)
        {
            ParseFunction(parser, token, false, out var _,
                out var name,
                out var arguments,
                out var otherArgs,
                out var body);

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
            var isOperator = false;

            // only statements can be named
            if (isStatement)
            {
                if (parser.MatchAndTake(TokenType.LeftParen))
                {
                    var operatorToken = parser.Take(TokenType.UserDefinedOperator);
                    parser.Take(TokenType.RightParen);

                    isOperator = true;
                    name = Lexer.GetOperatorIdentifier(operatorToken.Contents);
                }
                else
                {
                    name = parser.Take(TokenType.Identifier).Contents;
                }
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

            if (isOperator)
            {
                if (arguments.Count != 1 && arguments.Count != 2)
                    throw new MondCompilerException(token, CompilerError.IncorrectOperatorArity, arguments.Count);

                if (otherArgs != null)
                    throw new MondCompilerException(token, CompilerError.EllipsisInOperator);
            }

            // parse body
            if (parser.MatchAndTake(TokenType.Pointy))
            {
                body = new ScopeExpression(new List<Expression>
                {
                    new ReturnExpression(parser.Peek(), parser.ParseExpression())
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
                new ReturnExpression(parser.Peek(), parser.ParseExpression())
            });
        }
    }
}

using System.Text;
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
            bool isOperator;
            ScopeExpression body;

            ParseFunction(parser, token, true, out trailingSemicolon, out name, out arguments, out otherArgs, out isOperator, out body);
            var function = new FunctionExpression(token, isOperator ? null : name, arguments, otherArgs, body);

            return isOperator ? MakeOperator(name, function) : function;
        }

        public Expression Parse(Parser parser, Token token)
        {
            string name;
            List<string> arguments;
            string otherArgs;
            ScopeExpression body;
            bool isOperator;
            bool trailingSemicolon;

            ParseFunction(parser, token, false, out trailingSemicolon, out name, out arguments, out otherArgs, out isOperator, out body);
            var function = new FunctionExpression(token, isOperator ? null : name, arguments, otherArgs, body);

            return isOperator ? MakeOperator(name, function) : function;
        }

        public static void ParseFunction(
            Parser parser,
            Token token,
            bool isStatement,
            out bool trailingSemicolon,
            out string name,
            out List<string> arguments,
            out string otherArgs,
            out bool isOperator,
            out ScopeExpression body)
        {
            trailingSemicolon = false;

            name = null;
            arguments = new List<string>();
            otherArgs = null;
            isOperator = false;

            // only statements can be named
            if (isStatement)
            {
                if (parser.MatchAndTake(TokenType.LeftParen))
                {
                    var @operator = new StringBuilder();

                    do
                    {
                        @operator.Append(parser.Take(TokenSubType.Operator).Contents);

                        if (parser.Match(TokenType.RightParen))
                            break;

                        parser.Take(TokenType.Comma);
                    } while (!parser.Match(TokenType.RightParen));

                    parser.Take(TokenType.RightParen);

                    isOperator = true;
                    name = @operator.ToString();
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

            if (isOperator && arguments.Count != 1 && arguments.Count != 2)
                throw new MondCompilerException(token, CompilerError.IncorrectOperatorArity, arguments.Count);

            if (isOperator && otherArgs != null)
                throw new MondCompilerException(token, CompilerError.EllipsisInOperator);

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

        public static Expression MakeOperator(string @operator, FunctionExpression function)
        {
            var token = new Token(function.Token, TokenType.Global, "global", TokenSubType.Keyword);
            var global = new GlobalExpression(token);

            token = new Token(function.Token, TokenType.String, "$ops", TokenSubType.None);
            var opsString = new StringExpression(token, token.Contents);
            var opsField = new IndexerExpression(token, global, opsString);

            token = new Token(function.Token, TokenType.String, @operator, TokenSubType.Operator);
            var opIndex = new StringExpression(token, token.Contents);
            var opField = new IndexerExpression(token, opsField, opIndex);

            token = new Token(function.Token, TokenType.Assign, "=", TokenSubType.Operator);
            return new BinaryOperatorExpression(token, opField, function);
        }
    }
}

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
            var function = new FunctionExpression(token, name, arguments, otherArgs, isOperator, body);

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
            var function = new FunctionExpression(token, name, arguments, otherArgs, isOperator, body);

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

                    do @operator.Append(parser.Take(TokenSubType.Operator).Contents);
                    while (!parser.Match(TokenType.RightParen));

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

            if (isOperator)
            {
                if (arguments.Count != 1 && arguments.Count != 2)
                    throw new MondCompilerException(token, CompilerError.IncorrectOperatorArity, arguments.Count);

                if (otherArgs != null)
                    throw new MondCompilerException(token, CompilerError.EllipsisInOperator);

                // Check to see if we're trying to override any built-in unary prefix operators
                if (arguments.Count == 1 && Lexer.OperatorExists(name) && (name == "++" || name == "--" || name == "+" || name == "-" || name == "!" || name == "~"))
                    throw new MondCompilerException(token, CompilerError.CantOverrideBuiltInOperator, name);

                // Check to see if we're trying to override any built-in unary postfix or binary operators
                if (arguments.Count == 2 && Lexer.OperatorExists(name.ToString()) && name != "..." && name != "~")
                    throw new MondCompilerException(token, CompilerError.CantOverrideBuiltInOperator, name);
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

        public static Expression MakeOperator(string @operator, FunctionExpression function)
        {
            var token = new Token(function.Token, TokenType.Global, "global", TokenSubType.Keyword);
            var global = new GlobalExpression(token);

            token = new Token(function.Token, TokenType.Identifier, "__ops", TokenSubType.None);
            var opsField = new FieldExpression(token, global);

            token = new Token(function.Token, TokenType.String, @operator, TokenSubType.Operator);
            var opField = new FieldExpression(token, opsField);

            token = new Token(function.Token, TokenType.Assign, "=", TokenSubType.Operator);
            return new BinaryOperatorExpression(token, opField, function);
        }
    }
}

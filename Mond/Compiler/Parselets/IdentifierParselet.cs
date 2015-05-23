using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;
using Mond.Compiler.Parselets.Statements;

namespace Mond.Compiler.Parselets
{
    class IdentifierParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            if (parser.MatchAndTake(TokenType.Pointy)) // ident ->
                return ParseLambdaExpression(parser, token);

            return new IdentifierExpression(token);
        }

        private static Expression ParseLambdaExpression(Parser parser, Token token)
        {
            var arguments = new List<string>
            {
                token.Contents
            };

            var body = FunctionParselet.ParseLambdaExpressionBody(parser, token);
            return new FunctionExpression(token, null, arguments, null, false, body);
        }
    }
}

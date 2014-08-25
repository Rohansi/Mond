using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets
{
    class IdentifierParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            if (parser.MatchAndTake(TokenType.Pointy))  // ident ->
            {
                var arguments = new List<string>
                {
                    token.Contents
                };

                var body = new BlockExpression(new List<Expression>
                {
                    new ReturnExpression(token, parser.ParseExpession())
                });

                return new FunctionExpression(token, null, arguments, null, body);
            }

            return new IdentifierExpression(token);
        }
    }
}

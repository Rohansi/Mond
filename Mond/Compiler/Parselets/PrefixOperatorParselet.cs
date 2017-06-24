using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class PrefixOperatorParselet : IPrefixParselet
    {
        private readonly int _precedence;

        public PrefixOperatorParselet(int precedence)
        {
            _precedence = precedence;
        }

        public Expression Parse(Parser parser, Token token)
        {
            var right = parser.ParseExpression(_precedence);

            if (token.Type == TokenType.UserDefinedOperator)
            {
                var name = Lexer.GetOperatorIdentifier(token.Contents);
                var ident = new Token(token, TokenType.Identifier, name);
                var func = new IdentifierExpression(ident);
                return new CallExpression(token, func, new List<Expression> { right });
            }

            return new PrefixOperatorExpression(token, right);
        }
    }
}

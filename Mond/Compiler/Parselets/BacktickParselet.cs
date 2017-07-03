using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class BacktickParselet : IInfixParselet
    {
        public int Precedence => (int)PrecedenceValue.Relational; // same as UDOs

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var right = parser.ParseExpression(Precedence);
            var ident = new Token(token, TokenType.Identifier, token.Contents);
            var method = new IdentifierExpression(ident);
            return new CallExpression(token, method, new List<Expression> { left, right });
        }
    }
}

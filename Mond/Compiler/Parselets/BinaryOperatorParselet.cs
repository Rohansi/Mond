using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class BinaryOperatorParselet : IInfixParselet
    {
        private readonly int _precedence;
        private readonly bool _isRight;

        public int Precedence => _precedence;

        public BinaryOperatorParselet(int precedence, bool isRight)
        {
            _precedence = precedence;
            _isRight = isRight;
        }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var right = parser.ParseExpression(Precedence - (_isRight ? 1 : 0));

            if (token.Type == TokenType.UserDefinedOperator)
            {
                var name = Lexer.GetOperatorIdentifier(token.Contents);
                var ident = new Token(token, TokenType.Identifier, name);
                var func = new IdentifierExpression(ident);
                return new CallExpression(token, func, new List<Expression> { left, right });
            }

            return new BinaryOperatorExpression(token, left, right);
        }
    }
}

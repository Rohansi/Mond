using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class FieldParselet : IInfixParselet
    {
        public int Precedence => (int)PrecedenceValue.Postfix;

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var identifier = parser.Take(TokenType.Identifier);
            return new FieldExpression(identifier, left);
        }
    }
}

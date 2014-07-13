using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class FieldParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Postfix; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var identifier = parser.Take(TokenType.Identifier);
            return new FieldExpression(identifier, left);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class UnpackParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var right = parser.ParseExpession((int)PrecedenceValue.Prefix);
            return new UnpackExpression(token, right);
        }
    }
}

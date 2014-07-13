using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class NumberParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var value = double.Parse(token.Contents);
            return new NumberExpression(token, value);
        }
    }
}

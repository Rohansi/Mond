using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    interface IPrefixParselet
    {
        Expression Parse(Parser parser, Token token);
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    interface IInfixParselet
    {
        int Precedence { get; }

        Expression Parse(Parser parser, Expression left, Token token);
    }
}

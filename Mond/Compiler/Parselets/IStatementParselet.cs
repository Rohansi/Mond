using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    interface IStatementParselet
    {
        bool TrailingSemicolon { get; }

        Expression Parse(Parser parser, Token token);
    }
}

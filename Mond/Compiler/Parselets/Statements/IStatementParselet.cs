using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets.Statements
{
    interface IStatementParselet
    {
        Expression Parse(Parser parser, Token token, out bool trailingSemicolon);
    }
}

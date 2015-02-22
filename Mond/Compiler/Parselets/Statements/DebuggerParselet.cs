using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class DebuggerParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;
            return new DebuggerExpression(token);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class BreakParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return true; } }

        public Expression Parse(Parser parser, Token token)
        {
            return new BreakExpression(token);
        }
    }
}

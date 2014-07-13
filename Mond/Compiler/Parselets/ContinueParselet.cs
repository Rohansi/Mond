using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class ContinueParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return true; } }

        public Expression Parse(Parser parser, Token token)
        {
            return new ContinueExpression(token);
        }
    }
}

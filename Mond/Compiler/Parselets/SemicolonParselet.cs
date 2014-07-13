using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class SemicolonParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return false; } }

        public Expression Parse(Parser parser, Token token)
        {
            return new EmptyExpression(token);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class PostfixOperatorParselet : IInfixParselet
    {
        private readonly int _precedence;

        public int Precedence { get { return _precedence; } }

        public PostfixOperatorParselet(int precedence)
        {
            _precedence = precedence;
        }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            return new PostfixOperatorExpression(token, left);
        }
    }
}

using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    class PipelineParselet : IInfixParselet
    {
        public int Precedence { get { return (int)PrecedenceValue.Assign; } }

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            var right = parser.ParseExpression(Precedence);
            return new PipelineExpression(token, left, right);
        }
    }
}

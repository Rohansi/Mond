using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class SequenceParselet : IStatementParselet, IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            string name;
            List<string> arguments;
            string otherArgs;
            BlockExpression body;

            FunctionParselet.ParseFunction(parser, token, out trailingSemicolon, out name, out arguments, out otherArgs, out body);

            return new SequenceExpression(token, name, arguments, otherArgs, body);
        }

        public Expression Parse(Parser parser, Token token)
        {
            bool hasTrailingSemicolon;
            return Parse(parser, token, out hasTrailingSemicolon);
        }
    }
}

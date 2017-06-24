using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class SequenceParselet : IStatementParselet, IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            FunctionParselet.ParseFunction(parser, token, true, out trailingSemicolon,
                out var name,
                out var arguments,
                out var otherArgs,
                out var body);

            return new SequenceExpression(token, name, arguments, otherArgs, body);
        }

        public Expression Parse(Parser parser, Token token)
        {
            FunctionParselet.ParseFunction(parser, token, false, out var _,
                out var name,
                out var arguments,
                out var otherArgs,
                out var body);

            return new SequenceExpression(token, name, arguments, otherArgs, body);
        }
    }
}

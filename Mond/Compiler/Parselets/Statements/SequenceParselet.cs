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
            bool isOperator;
            ScopeExpression body;

            FunctionParselet.ParseFunction(parser, token, true, out trailingSemicolon, out name, out arguments, out otherArgs, out isOperator, out body);
            var sequence = new SequenceExpression(token, isOperator ? null : name, arguments, otherArgs, body);

            return isOperator ? FunctionParselet.MakeOperator(name, sequence) : sequence;
        }

        public Expression Parse(Parser parser, Token token)
        {
            string name;
            List<string> arguments;
            string otherArgs;
            bool isOperator;
            ScopeExpression body;
            bool trailingSemicolon;

            FunctionParselet.ParseFunction(parser, token, false, out trailingSemicolon, out name, out arguments, out otherArgs, out isOperator, out body);
            var sequence = new SequenceExpression(token, isOperator ? null : name, arguments, otherArgs, body);

            return isOperator ? FunctionParselet.MakeOperator(name, sequence) : sequence;
        }
    }
}

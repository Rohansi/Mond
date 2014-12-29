using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Visitors
{
    class LoopContainsFunctionVisitor : ExpressionVisitor<int>
    {
        public bool Value { get; private set; }

        public LoopContainsFunctionVisitor()
        {
            Value = false;
        }

        public override int Visit(FunctionExpression expression)
        {
            Value = true;
            return 0;
        }

        public override int Visit(SequenceExpression expression)
        {
            Value = true;
            return 0;
        }

        public override int Visit(ListComprehensionExpression expression)
        {
            // List comprehensions are just immediately invoked sequences
            Value = true;
            return 0;
        }
    }
}

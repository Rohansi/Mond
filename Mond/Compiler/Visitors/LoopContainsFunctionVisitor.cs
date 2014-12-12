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

        public override int Visit(DoWhileExpression expression)
        {
            // DoWhile condition is part of the loop, don't visit it
            return 0;
        }

        public override int Visit(ForeachExpression expression)
        {
            expression.Expression.Accept(this);
            return 0;
        }

        public override int Visit(ForExpression expression)
        {
            if (expression.Initializer != null)
                expression.Initializer.Accept(this);

            return 0;
        }

        public override int Visit(WhileExpression expression)
        {
            // While condition is part of the loop, don't visit it
            return 0;
        }
    }
}

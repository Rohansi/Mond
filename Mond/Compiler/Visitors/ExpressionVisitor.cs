using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler
{
    abstract class ExpressionVisitor<T> : IExpressionVisitor<T>
    {
        #region Statements

        public virtual T Visit(BreakExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(ContinueExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(DoWhileExpression expression)
        {
            expression.Block.Accept(this);
            expression.Condition.Accept(this);

            return default(T);
        }

        public virtual T Visit(ForeachExpression expression)
        {
            expression.Expression.Accept(this);
            expression.Block.Accept(this);

            return default(T);
        }

        public virtual T Visit(ForExpression expression)
        {
            expression.Initializer.Accept(this);
            expression.Condition.Accept(this);
            expression.Block.Accept(this);
            expression.Increment.Accept(this);

            return default(T);
        }

        public virtual T Visit(FunctionExpression expression)
        {
            expression.Block.Accept(this);

            return default(T);
        }

        public virtual T Visit(IfExpression expression)
        {
            foreach (var branch in expression.Branches)
            {
                branch.Condition.Accept(this);
                branch.Block.Accept(this);
            }

            if (expression.Else != null)
                expression.Else.Block.Accept(this);

            return default(T);
        }

        public virtual T Visit(ReturnExpression expression)
        {
            expression.Value.Accept(this);

            return default(T);
        }

        public virtual T Visit(SequenceExpression expression)
        {
            expression.Block.Accept(this);

            return default(T);
        }

        public virtual T Visit(SwitchExpression expression)
        {
            foreach (var branch in expression.Branches)
            {
                foreach (var branchCondition in branch.Conditions)
                {
                    branchCondition.Accept(this);
                }

                branch.Block.Accept(this);
            }

            if (expression.DefaultBlock != null)
                expression.DefaultBlock.Accept(this);

            return default(T);
        }

        public virtual T Visit(VarExpression expression)
        {
            foreach (var decl in expression.Declarations.Where(d => d.Initializer != null))
            {
                decl.Initializer.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(WhileExpression expression)
        {
            expression.Condition.Accept(this);
            expression.Block.Accept(this);

            return default(T);
        }

        public virtual T Visit(YieldExpression expression)
        {
            expression.Value.Accept(this);

            return default(T);
        }

        #endregion

        public virtual T Visit(ArrayExpression expression)
        {
            foreach (var value in expression.Values)
            {
                value.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(BinaryOperatorExpression expression)
        {
            expression.Left.Accept(this);
            expression.Right.Accept(this);

            return default(T);
        }

        public virtual T Visit(BlockExpression expression)
        {
            foreach (var expr in expression.Statements)
            {
                expr.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(BoolExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(CallExpression expression)
        {
            expression.Method.Accept(this);

            foreach (var arg in expression.Arguments)
            {
                arg.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(EmptyExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(FieldExpression expression)
        {
            expression.Left.Accept(this);

            return default(T);
        }

        public virtual T Visit(GlobalExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(IdentifierExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(IndexerExpression expression)
        {
            expression.Left.Accept(this);
            expression.Index.Accept(this);

            return default(T);
        }

        public virtual T Visit(ListComprehensionExpression expression)
        {
            expression.Body.Accept(this);

            return default(T);
        }

        public virtual T Visit(NullExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(NumberExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(ObjectExpression expression)
        {
            foreach (var value in expression.Values)
            {
                value.Value.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(PipelineExpression expression)
        {
            expression.Left.Accept(this);
            expression.Right.Accept(this);

            return default(T);
        }

        public virtual T Visit(PostfixOperatorExpression expression)
        {
            expression.Left.Accept(this);

            return default(T);
        }

        public virtual T Visit(PrefixOperatorExpression expression)
        {
            expression.Right.Accept(this);

            return default(T);
        }

        public virtual T Visit(ScopeExpression expression)
        {
            foreach (var expr in expression.Statements)
            {
                expr.Accept(this);
            }

            return default(T);
        }

        public virtual T Visit(SliceExpression expression)
        {
            expression.Left.Accept(this);

            if (expression.Start != null)
                expression.Start.Accept(this);

            if (expression.End != null)
                expression.End.Accept(this);

            if (expression.Step != null)
                expression.Step.Accept(this);

            return default(T);
        }

        public virtual T Visit(StringExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(TernaryExpression expression)
        {
            expression.Condition.Accept(this);
            expression.IfTrue.Accept(this);
            expression.IfFalse.Accept(this);

            return default(T);
        }

        public virtual T Visit(UndefinedExpression expression)
        {
            return default(T);
        }

        public virtual T Visit(UnpackExpression expression)
        {
            expression.Right.Accept(this);

            return default(T);
        }
    }
}

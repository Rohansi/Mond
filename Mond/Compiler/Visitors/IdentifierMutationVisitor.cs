using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler
{
    /// <summary>
    /// Detects whether an expression could write to a given identifier name.
    ///
    /// This is used by compound assignment (<c>x += y</c>) to check that the right side does not
    /// modify the variable being assigned to. Normally the variable is loaded before the right
    /// side is evaluated, but the in-place instructions read it afterwards, so the two are only
    /// equivalent when the right side leaves the variable alone.
    /// </summary>
    internal sealed class IdentifierMutationVisitor : ExpressionVisitor<object>
    {
        private readonly string _name;

        private IdentifierMutationVisitor(string name)
        {
            _name = name;
        }

        private bool Found { get; set; }

        public static bool Mutates(Expression expression, string name)
        {
            var visitor = new IdentifierMutationVisitor(name);
            expression.Accept(visitor);
            return visitor.Found;
        }

        public override object Visit(BinaryOperatorExpression expression)
        {
            if (expression.IsAssign)
                CheckTarget(expression.Left);

            return base.Visit(expression);
        }

        public override object Visit(PrefixOperatorExpression expression)
        {
            if (expression.Operation is TokenType.Increment or TokenType.Decrement)
                CheckTarget(expression.Right);

            return base.Visit(expression);
        }

        public override object Visit(PostfixOperatorExpression expression)
        {
            CheckTarget(expression.Left);

            return base.Visit(expression);
        }

        public override object Visit(VarExpression expression)
        {
            // a nested declaration of the same name shadows ours, so writes found below this point
            // may not refer to the same variable - bail out rather than try to track scopes
            foreach (var declaration in expression.Declarations)
            {
                if (declaration.Name == _name)
                    Found = true;
            }

            return base.Visit(expression);
        }

        private void CheckTarget(Expression target)
        {
            if (target is IdentifierExpression identifier && identifier.Name == _name)
                Found = true;
        }
    }
}

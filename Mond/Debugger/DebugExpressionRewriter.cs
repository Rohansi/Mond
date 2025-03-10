using Mond.Compiler;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;
using Mond.Compiler.Visitors;

namespace Mond.Debugger
{
    internal class DebugExpressionRewriter : ExpressionRewriteVisitor
    {
        private readonly string _localAccessor;
        private Scope _scope;

        public DebugExpressionRewriter(string localAccessor)
        {
            _localAccessor = localAccessor;
            _scope = new Scope(0, 0, 0, null);
        }

        public override Expression Visit(IdentifierExpression expression)
        {
            if (_scope.IsDefined(expression.Name))
                return base.Visit(expression);

            var blankToken = new Token(expression.Token, TokenType.Eof, null);
            var accessorToken = new Token(expression.Token, TokenType.Identifier, _localAccessor);

            var accessor = new FieldExpression(accessorToken, new GlobalExpression(blankToken));

            return new IndexerExpression(blankToken, accessor, new StringExpression(blankToken, expression.Name));
        }

        public override Expression Visit(ScopeExpression expression)
        {
            PushScope();
            var result = base.Visit(expression);
            PopScope();

            return result;
        }

        public override Expression Visit(VarExpression expression)
        {
            foreach (var def in expression.Declarations)
            {
                _scope.Define(def.Name, false);
            }

            return base.Visit(expression);
        }

        public override Expression Visit(DestructuredArrayExpression expression)
        {
            foreach (var index in expression.Indices)
            {
                _scope.Define(index.Name, false);
            }

            return base.Visit(expression);
        }

        public override Expression Visit(DestructuredObjectExpression expression)
        {
            foreach (var field in expression.Fields)
            {
                _scope.Define(field.Alias ?? field.Name, false);
            }

            return base.Visit(expression);
        }

        public override Expression Visit(FunctionExpression expression)
        {
            if (expression.Name != null)
                _scope.Define(expression.Name, false);

            PushScope();

            foreach (var arg in expression.Arguments)
            {
                _scope.Define(arg, false);
            }

            if (expression.OtherArguments != null)
                _scope.Define(expression.OtherArguments, false);

            var result = base.Visit(expression);
            PopScope();

            return result;
        }

        public override Expression Visit(SequenceExpression expression)
        {
            if (expression.Name != null)
                _scope.Define(expression.Name, false);

            PushScope();

            foreach (var arg in expression.Arguments)
            {
                _scope.Define(arg, false);
            }

            if (expression.OtherArguments != null)
                _scope.Define(expression.OtherArguments, false);

            var result = base.Visit(expression);
            PopScope();

            return result;
        }

        public override Expression Visit(ForeachExpression expression)
        {
            PushScope();

            if (expression.DestructureExpression == null)
                _scope.Define(expression.Identifier, false);

            var result = base.Visit(expression);
            PopScope();

            return result;
        }

        public override Expression Visit(ForExpression expression)
        {
            PushScope();
            var result = base.Visit(expression);
            PopScope();

            return result;
        }

        private void PushScope()
        {
            _scope = new Scope(0, 0, 0, _scope);
        }

        private void PopScope()
        {
            _scope = _scope.Previous;
        }
    }
}

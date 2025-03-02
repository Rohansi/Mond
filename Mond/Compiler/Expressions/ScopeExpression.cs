using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    internal class ScopeExpression : BlockExpression
    {
        private Scope _innerScope;

        public ScopeExpression(BlockExpression block)
            : base(block.Token, block.Statements)
        {
            
        }

        public ScopeExpression(Token token, IList<Expression> statements)
            : base(token, statements)
        {

        }

        public ScopeExpression(IList<Expression> statements)
            : base(statements)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.PushScope(_innerScope);
            var stack = base.Compile(context);
            context.PopScope();

            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            _innerScope = context.PushScope();
            var result = base.Simplify(context);
            context.PopScope();
            return result;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

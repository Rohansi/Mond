using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    class ScopeExpression : BlockExpression
    {
        public ScopeExpression(BlockExpression block)
            : base(block.Statements)
        {
            
        }

        public ScopeExpression(IList<Expression> statements)
            : base(statements)
        {
            
        }

        public override int Compile(CompilerContext context)
        {
            context.PushScope();
            var stack = base.Compile(context);
            context.PopScope();

            return stack;
        }
    }
}

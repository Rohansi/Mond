﻿using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    class ScopeExpression : BlockExpression
    {
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
            context.PushScope();
            var stack = base.Compile(context);
            context.PopScope();

            return stack;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

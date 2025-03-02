namespace Mond.Compiler.Expressions.Statements
{
    internal class ForExpression : Expression, IStatementExpression
    {
        public BlockExpression Initializer { get; private set; }
        public Expression Condition { get; private set; }
        public BlockExpression Increment { get; private set; }
        public ScopeExpression Block { get; private set; }

        public bool HasChildren => true;

        public ForExpression(Token token, BlockExpression initializer, Expression condition, BlockExpression increment, ScopeExpression block)
            : base(token)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Block = block;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var start = context.MakeLabel("forStart");
            var increment = context.MakeLabel("forContinue");
            var end = context.MakeLabel("forEnd");

            if (Initializer != null)
            {
                stack += Initializer.Compile(context);
            }

            // loop body
            context.Bind(start);

            if (Condition != null)
            {
                context.Statement(Condition);
                stack += Condition.Compile(context);
                stack += context.JumpFalse(end);
            }

            context.PushLoop(increment, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Bind(increment); // continue

            if (Increment != null)
            {
                // no need to output a statement, block will do it for us
                stack += Increment.Compile(context);
            }

            stack += context.Jump(start);

            stack += context.Bind(end); // break

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Initializer = (BlockExpression)Initializer?.Simplify(context);
            Condition = Condition?.Simplify(context);
            Increment = (BlockExpression)Increment?.Simplify(context);
            Block = (ScopeExpression)Block.Simplify(context);

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Initializer?.SetParent(this);
            Condition?.SetParent(this);
            Increment?.SetParent(this);
            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

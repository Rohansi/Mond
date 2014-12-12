namespace Mond.Compiler.Expressions.Statements
{
    class ForExpression : Expression, IStatementExpression
    {
        public BlockExpression Initializer { get; private set; }
        public Expression Condition { get; private set; }
        public BlockExpression Increment { get; private set; }
        public BlockExpression Block { get; private set; }

        public ForExpression(Token token, BlockExpression initializer, Expression condition, BlockExpression increment, BlockExpression block)
            : base(token.FileName, token.Line)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Block = block;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var start = context.MakeLabel("forStart");
            var increment = context.MakeLabel("forContinue");
            var end = context.MakeLabel("forEnd");

            context.PushScope();

            if (Initializer != null)
                stack += Initializer.Compile(context);

            context.Bind(start);
            if (Condition != null)
            {
                stack += Condition.Compile(context);
                stack += context.JumpFalse(end);
            }

            context.PushLoop(increment, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Bind(increment);

            if (Increment != null)
                stack += Increment.Compile(context);

            stack += context.Jump(start);

            stack += context.Bind(end);

            context.PopScope();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            if (Initializer != null)
                Initializer = (BlockExpression)Initializer.Simplify();

            if (Condition != null)
                Condition = Condition.Simplify();

            if (Increment != null)
                Increment = (BlockExpression)Increment.Simplify();

            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Initializer != null)
                Initializer.SetParent(this);

            if (Condition != null)
                Condition.SetParent(this);

            if (Increment != null)
                Increment.SetParent(this);

            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

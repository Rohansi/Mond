namespace Mond.Compiler.Expressions.Statements
{
    internal class WhileExpression : Expression, IStatementExpression
    {
        public Expression Condition { get; private set; }
        public ScopeExpression Block { get; private set; }

        public bool HasChildren => true;

        public WhileExpression(Token token, Expression condition, ScopeExpression block)
            : base(token)
        {
            Condition = condition;
            Block = block;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var start = context.MakeLabel("whileStart");
            var end = context.MakeLabel("whileEnd");

            var boolExpression = Condition as BoolExpression;
            var isInfinite = boolExpression != null && boolExpression.Value;

            stack += context.Bind(start); // continue

            if (!isInfinite)
            {
                context.Statement(Condition);
                stack += Condition.Compile(context);
                stack += context.JumpFalse(end);
            }

            context.PushLoop(start, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Jump(start);

            stack += context.Bind(end); // break

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Condition = Condition.Simplify(context);
            Block = (ScopeExpression)Block.Simplify(context);

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Condition.SetParent(this);
            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

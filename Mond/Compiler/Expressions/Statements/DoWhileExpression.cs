namespace Mond.Compiler.Expressions.Statements
{
    internal class DoWhileExpression : Expression, IStatementExpression
    {
        public ScopeExpression Block { get; private set; }
        public Expression Condition { get; private set; }

        public bool HasChildren => true;

        public DoWhileExpression(Token token, ScopeExpression block, Expression condition)
            : base(token)
        {
            Block = block;
            Condition = condition;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var start = context.MakeLabel("doWhileStart");
            var cont = context.MakeLabel("doWhileContinue");
            var end = context.MakeLabel("doWhileEnd");

            // body
            stack += context.Bind(start);

            context.PushLoop(cont, end);
            stack += Block.Compile(context);
            context.PopLoop();

            // condition check
            stack += context.Bind(cont); // continue

            context.Statement(Condition);
            stack += Condition.Compile(context);
            stack += context.JumpTrue(start);

            stack += context.Bind(end); // break

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Block = (ScopeExpression)Block.Simplify(context);
            Condition = Condition.Simplify(context);
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Block.SetParent(this);
            Condition.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

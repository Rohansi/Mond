using Mond.Compiler.Visitors;

namespace Mond.Compiler.Expressions.Statements
{
    class WhileExpression : Expression, IStatementExpression
    {
        public Expression Condition { get; private set; }
        public BlockExpression Block { get; private set; }

        public WhileExpression(Token token, Expression condition, BlockExpression block)
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
            var cont = context.MakeLabel("whileContinue");
            var brk = context.MakeLabel("whileBreak");
            var end = context.MakeLabel("whileEnd");

            var boolExpression = Condition as BoolExpression;
            var isInfinite = boolExpression != null && boolExpression.Value;

            var containsFunction = new LoopContainsFunctionVisitor();
            Block.Accept(containsFunction);

            var loopContext = containsFunction.Value ? new LoopContext(context) : context;

            context.PushScope();

            stack += context.Bind(start); // continue (without function)

            if (!isInfinite)
            {
                stack += Condition.Compile(context);
                stack += context.JumpFalse(end);
            }

            loopContext.PushLoop(containsFunction.Value ? cont : start, containsFunction.Value ? brk : end);

            if (containsFunction.Value)
                stack += loopContext.Enter();

            stack += Block.Compile(loopContext);

            if (containsFunction.Value)
            {
                stack += loopContext.Bind(cont); // continue (with function)
                stack += loopContext.Leave();
            }

            loopContext.PopLoop();

            stack += context.Jump(start);

            if (containsFunction.Value)
            {
                stack += context.Bind(brk); // break (with function)
                stack += context.Leave();
            }

            stack += context.Bind(end); // break (without function)

            context.PopScope();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Condition = Condition.Simplify();
            Block = (BlockExpression)Block.Simplify();

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

using System.Linq;
using Mond.Compiler.Visitors;

namespace Mond.Compiler.Expressions.Statements
{
    class ForExpression : Expression, IStatementExpression
    {
        public BlockExpression Initializer { get; private set; }
        public Expression Condition { get; private set; }
        public BlockExpression Increment { get; private set; }
        public BlockExpression Block { get; private set; }

        public bool HasChildren => true;

        public ForExpression(Token token, BlockExpression initializer, Expression condition, BlockExpression increment, BlockExpression block)
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
            var brk = context.MakeLabel("forBreak");
            var end = context.MakeLabel("forEnd");

            var containsFunction = new LoopContainsFunctionVisitor();
            Block.Accept(containsFunction);

            context.PushScope();

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

            var loopContext = containsFunction.Value ? new LoopContext(context) : context;

            loopContext.PushLoop(increment, containsFunction.Value ? brk : end);

            if (containsFunction.Value)
                stack += loopContext.Enter();

            stack += Block.Compile(loopContext);

            stack += context.Bind(increment); // continue

            if (containsFunction.Value)
                stack += loopContext.Leave();

            loopContext.PopLoop();

            if (Increment != null)
            {
                // no need to output a statement, block will do it for us
                stack += Increment.Compile(context);
            }

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
            Initializer = (BlockExpression)Initializer?.Simplify();
            Condition = Condition?.Simplify();
            Increment = (BlockExpression)Increment?.Simplify();
            Block = (BlockExpression)Block.Simplify();

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

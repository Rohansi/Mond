namespace Mond.Compiler.Expressions.Statements
{
    class DoWhileExpression : Expression, IStatementExpression
    {
        public BlockExpression Block { get; private set; }
        public Expression Condition { get; private set; }

        public DoWhileExpression(Token token, BlockExpression block, Expression condition)
            : base(token.FileName, token.Line)
        {
            Block = block;
            Condition = condition;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var start = context.MakeLabel("doWhileStart");
            var cont = context.MakeLabel("doWhileContinue");
            var end = context.MakeLabel("doWhileEnd");

            stack += context.Bind(start);

            context.PushLoop(cont, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Bind(cont);
            stack += Condition.Compile(context);
            stack += context.JumpTrue(start);
            stack += context.Bind(end);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Block = (BlockExpression)Block.Simplify();
            Condition = Condition.Simplify();

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

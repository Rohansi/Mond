namespace Mond.Compiler.Expressions
{
    class SliceExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Start { get; private set; }
        public Expression End { get; private set; }
        public Expression Step { get; private set; }

        public override Token StartToken => Left.StartToken;

        public SliceExpression(Token token, Expression left, Expression start, Expression end, Expression step)
            : base(token)
        {
            Left = left;
            Start = start;
            End = end;
            Step = step;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;

            stack += Left.Compile(context);

            if (Start != null)
                stack += Start.Compile(context);
            else
                stack += context.LoadUndefined();

            if (End != null)
                stack += End.Compile(context);
            else
                stack += context.LoadUndefined();

            if (Step != null)
                stack += Step.Compile(context);
            else
                stack += context.LoadUndefined();

            context.Position(Token); // debug info
            stack += context.Slice();

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Left = Left.Simplify(context);
            Start = Start?.Simplify(context);
            End = End?.Simplify(context);
            Step = Step?.Simplify(context);

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
            Start?.SetParent(this);
            End?.SetParent(this);
            Step?.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

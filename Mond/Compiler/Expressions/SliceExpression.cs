namespace Mond.Compiler.Expressions
{
    class SliceExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Start { get; private set; }
        public Expression End { get; private set; }
        public Expression Step { get; private set; }

        public SliceExpression(Token token, Expression left, Expression start, Expression end, Expression step)
            : base(token.FileName, token.Line, token.Column)
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

            context.Position(Line, Column); // debug info
            stack += context.Slice();

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();

            if (Start != null)
                Start = Start.Simplify();

            if (End != null)
                End = End.Simplify();

            if (Step != null)
                Step = Step.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);

            if (Start != null)
                Start.SetParent(this);

            if (End != null)
                End.SetParent(this);

            if (Step != null)
                Step.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

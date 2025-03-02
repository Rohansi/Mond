namespace Mond.Compiler.Expressions
{
    class TernaryExpression : Expression
    {
        public Expression Condition { get; private set; }
        public Expression IfTrue { get; private set; }
        public Expression IfFalse { get; private set; }

        public override Token StartToken => Condition.StartToken;

        public TernaryExpression(Token token, Expression condition, Expression ifTrue, Expression ifFalse)
            : base(token)
        {
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var falseLabel = context.MakeLabel("ternaryFalse");
            var endLabel = context.MakeLabel("ternaryEnd");

            stack += Condition.Compile(context);
            stack += context.JumpFalse(falseLabel);
            CheckStack(IfTrue.Compile(context), 1);
            stack += context.Jump(endLabel);
            stack += context.Bind(falseLabel);
            CheckStack(IfFalse.Compile(context), 1);
            stack += context.Bind(endLabel);

            CheckStack(stack, 0);
            return 1;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Condition = Condition.Simplify(context);
            IfTrue = IfTrue.Simplify(context);
            IfFalse = IfFalse.Simplify(context);

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Condition.SetParent(this);
            IfTrue.SetParent(this);
            IfFalse.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Expressions
{
    class YieldExpression : Expression
    {
        public Expression Value { get; private set; }

        public YieldExpression(Token token, Expression value)
            : base(token)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var sequenceContext = context.Root as SequenceBodyContext;
            if (sequenceContext == null)
                throw new MondCompilerException(this, CompilerError.YieldInFun);

            var state = sequenceContext.SequenceBody.State;
            var enumerable = sequenceContext.SequenceBody.Enumerable;

            var stack = 0;
            var nextState = sequenceContext.SequenceBody.NextState;
            var nextStateLabel = sequenceContext.SequenceBody.MakeStateLabel(context);

            stack += Value.Compile(context);
            stack += context.Load(enumerable);
            stack += context.StoreField(context.String("current"));

            stack += context.Load(context.Number(nextState)); // set resume point
            stack += context.Store(state);

            stack += context.StoreState(sequenceContext.LocalIndex - 1); // save locals
            stack += context.LoadTrue();
            stack += context.Return();

            stack += context.Bind(nextStateLabel);
            stack += context.LoadState(sequenceContext.LocalIndex - 1); // load locals

            if (!(Parent is IBlockExpression))
            {
                stack += context.Load(context.Identifier("#input"));
                CheckStack(stack, 1);
                return stack;
            }

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Value = Value.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Value.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

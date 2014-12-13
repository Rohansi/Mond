namespace Mond.Compiler.Expressions.Statements
{
    class YieldExpression : Expression, IStatementExpression
    {
        public Expression Value { get; private set; }

        public YieldExpression(Token token, Expression value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var sequenceContext = context.Root as SequenceBodyContext;
            if (sequenceContext == null)
                throw new MondCompilerException(FileName, Line, CompilerError.YieldInFun);

            var state = sequenceContext.SequenceBody.State;
            var enumerable = sequenceContext.SequenceBody.Enumerable;

            var stack = 0;
            var nextState = sequenceContext.SequenceBody.NextState;
            var nextStateLabel = sequenceContext.SequenceBody.MakeStateLabel(context);

            stack += context.Load(context.Number(nextState));
            stack += context.Store(state);

            stack += Value.Compile(context);
            stack += context.Load(enumerable);
            stack += context.StoreField(context.String("current"));

            stack += context.StoreLocals(sequenceContext.LocalIndex - 1); // save locals
            stack += context.LoadTrue();
            stack += context.Return();

            stack += context.Bind(nextStateLabel);
            stack += context.LoadLocals(sequenceContext.LocalIndex - 1); // load locals

            if (!(Parent is IBlockExpression))
            {
                var parentBinOp = Parent as BinaryOperatorExpression;
                if ((parentBinOp != null && parentBinOp.IsAssign) || Parent is VarExpression)
                {
                    stack += context.Load(context.Identifier("#input"));
                    CheckStack(stack, 1);
                    return stack;
                }

                // TODO: allow more cases? can't yield with stuff on the stack
                throw new MondCompilerException(FileName, Line, CompilerError.YieldNotInAssign);
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

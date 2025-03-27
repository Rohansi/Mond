namespace Mond.Compiler.Expressions.Statements
{
    class ReturnExpression : Expression, IStatementExpression
    {
        public Expression Value { get; private set; }

        public bool HasChildren => false;

        public override Token EndToken
        {
            get => base.EndToken ?? Value.EndToken;
            set => base.EndToken = value;
        }

        public ReturnExpression(Token token, Expression value)
            : base(token)
        {
            Value = value ?? new UndefinedExpression(token);
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;

            if (context.Root is SequenceBodyContext sequenceContext)
            {
                var sequenceBody = sequenceContext.SequenceBody;

                stack += Value.Compile(context);
                stack += context.Load(sequenceBody.Enumerable);
                stack += context.StoreField(context.String("current"));
                
                stack += context.Jump(sequenceBody.EndLabel);

                CheckStack(stack, 0);
                return stack;
            }

            if (context.AssignedName != null)
            {
                if (Value is CallExpression callExpression)
                {
                    if (callExpression.Method is IdentifierExpression identifierExpression &&
                        context.Identifier(identifierExpression.Name) == context.AssignedName)
                    {
                        stack += callExpression.CompileTailCall(context);
                        CheckStack(stack, 0);
                        return stack;
                    }
                }
            }

            stack += Value.Compile(context);
            stack += context.Return();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Value = Value?.Simplify(context);
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);
            Value?.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

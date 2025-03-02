namespace Mond.Compiler.Expressions.Statements
{
    internal class ForeachExpression : Expression, IStatementExpression
    {
        public Token InToken { get; }
        public string Identifier { get; }
        public Expression Expression { get; private set; }
        public ScopeExpression Block { get; private set; }
        public Expression DestructureExpression { get; private set; }

        public bool HasChildren => true;

        public ForeachExpression(Token token, Token inToken, string identifier, Expression expression, ScopeExpression block, Expression destructure = null)
            : base(token)
        {
            InToken = inToken;
            Identifier = identifier;
            Expression = expression;
            Block = block;
            DestructureExpression = destructure;
        }

        public override int Compile(FunctionContext context)
        {
            var identifier = DestructureExpression != null
                ? context.DefineInternal(Identifier, true)
                : context.Identifier(Identifier);

            context.Position(Token);

            var stack = 0;
            var start = context.MakeLabel("foreachStart");
            var end = context.MakeLabel("foreachEnd");

            var enumerator = context.DefineInternal("enumerator", true);

            // set enumerator
            context.Statement(Expression);
            stack += Expression.Compile(context);
            stack += context.InstanceCall(context.String("getEnumerator"), 0, []);
            stack += context.Store(enumerator);

            // loop body
            stack += context.Bind(start); // continue

            // loop while moveNext returns true
            context.Statement(InToken, InToken);
            stack += context.Load(enumerator);
            stack += context.InstanceCall(context.String("moveNext"), 0, []);
            stack += context.JumpFalse(end);

            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("current"));
            stack += context.Store(identifier);

            if (DestructureExpression != null)
            {
                stack += context.Load(identifier);
                stack += DestructureExpression.Compile(context);
            }
            
            context.PushLoop(start, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Jump(start);

            // after loop
            stack += context.Bind(end); // break
            stack += context.Load(enumerator);
            stack += context.InstanceCall(context.String("dispose"), 0, []);
            stack += context.Drop();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            if (!context.DefineIdentifier(Identifier))
            {
                throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, Identifier);
            }

            Expression = Expression.Simplify(context);
            DestructureExpression = DestructureExpression?.Simplify(context);
            Block = (ScopeExpression)Block.Simplify(context);

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Expression.SetParent(this);
            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

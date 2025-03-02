namespace Mond.Compiler.Expressions
{
    class IdentifierExpression : Expression, IStorableExpression
    {
        public string Name { get; }

        public IdentifierExpression(Token token)
            : base(token)
        {
            Name = token.Contents;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;
            var identifier = context.Identifier(Name);

            // anything that starts with 'op_' is always an implicit global
            if (!context.Compiler.Options.UseImplicitGlobals && identifier == null && !Name.StartsWith("op_"))
                throw new MondCompilerException(this, CompilerError.UndefinedIdentifier, Name);

            context.Position(Token); // debug info

            if (identifier == null)
            {
                stack += context.LoadGlobal();
                stack += context.LoadField(context.String(Name));
            }
            else
            {
                stack += context.Load(identifier);
            }

            CheckStack(stack, 1);
            return stack;
        }

        public int CompilePreLoadStore(FunctionContext context, int times)
        {
            return 0;
        }

        public int CompileLoad(FunctionContext context)
        {
            return Compile(context);
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;
            var identifier = context.Identifier(Name);

            if (!context.Compiler.Options.UseImplicitGlobals && identifier == null && !Name.StartsWith("op_"))
                throw new MondCompilerException(this, CompilerError.UndefinedIdentifier, Name);

            if (identifier == null)
            {
                stack += context.LoadGlobal();

                context.Position(Token); // debug info
                stack += context.StoreField(context.String(Name));
            }
            else
            {
                if (identifier.IsReadOnly)
                    throw new MondCompilerException(this, CompilerError.CantModifyReadonlyVar, Name);

                context.Position(Token); // debug info
                stack += context.Store(identifier);
            }

            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
        
        public bool SupportsIncDecF(FunctionContext context, out IdentifierOperand operand)
        {
            return context.TryGetIdentifier(Name, out operand) &&
                   operand.FrameIndex == context.Depth &&
                   !operand.IsReadOnly;
        }
    }
}

namespace Mond.Compiler.Expressions
{
    class IdentifierExpression : Expression, IStorableExpression
    {
        public string Name { get; private set; }

        public IdentifierExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            Name = token.Contents;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;
            var identifier = context.Identifier(Name);

            if (!context.Compiler.Options.UseImplicitGlobals && identifier == null)
                throw new MondCompilerException(this, CompilerError.UndefinedIdentifier, Name);

            context.Position(FileName, Line, Column); // debug info

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

            if (!context.Compiler.Options.UseImplicitGlobals && identifier == null)
                throw new MondCompilerException(this, CompilerError.UndefinedIdentifier, Name);

            if (identifier == null)
            {
                stack += context.LoadGlobal();

                context.Position(FileName, Line, Column); // debug info
                stack += context.StoreField(context.String(Name));
            }
            else
            {
                if (identifier.IsReadOnly)
                    throw new MondCompilerException(this, CompilerError.CantModifyReadonlyVar, Name);

                context.Position(FileName, Line, Column); // debug info
                stack += context.Store(identifier);
            }

            return stack;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    internal class FunctionExpression : Expression, IStatementExpression, IDeclarationExpression
    {
        public string Name { get; }
        public ReadOnlyCollection<string> Arguments { get; }
        public string OtherArguments { get; }
        public ScopeExpression Block { get; private set; }

        public string DebugName { get; set; }

        public bool HasChildren => false;

        public IEnumerable<string> DeclaredIdentifiers => Name != null ? new[] { Name } : [];

        private Scope _functionScope;

        public FunctionExpression(
            Token token, string name, List<string> arguments, string otherArgs, ScopeExpression block, string debugName = null)
            : base(token)
        {
            Name = name;
            Arguments = arguments.AsReadOnly();
            OtherArguments = otherArgs;
            Block = block;

            DebugName = debugName;
        }

        public virtual void CompileBody(FunctionContext context)
        {
            var stack = 0;

            if (OtherArguments != null)
                stack += context.VarArgs(Arguments.Count);

            stack += Block.Compile(context);

            stack += context.LoadUndefined();
            stack += context.Return();

            CheckStack(stack, 0);
        }

        public override int Compile(FunctionContext context)
        {
            var isStatement = Parent is IBlockExpression;
            var shouldBeGlobal = context.MakeDeclarationsGlobal;
            var shouldStore = Name != null;

            var identifier = shouldStore && !shouldBeGlobal
                ? context.Identifier(Name)
                : null;

            // compile body
            var functionContext = context.MakeFunction(Name ?? DebugName, _functionScope);
            functionContext.Position(Token);
            CompileBody(functionContext);
            functionContext.PopScope();

            // assign result
            var stack = 0;

            context.Position(Token); // debug info
            stack += context.Closure(functionContext.Label);

            if (shouldStore)
            {
                if (!isStatement) // statements should leave nothing on the stack
                    stack += context.Dup();

                if (!shouldBeGlobal)
                {
                    stack += context.Store(identifier);
                }
                else
                {
                    stack += context.LoadGlobal();
                    stack += context.StoreField(context.String(Name));
                }

                if (isStatement)
                {
                    CheckStack(stack, 0);
                    return stack;
                }
            }

            CheckStack(stack, 1);
            return stack;
        }

        protected virtual void SimplifyBody(SimplifyContext context)
        {
            Block = (ScopeExpression)Block.Simplify(context);
        }

        public override Expression Simplify(SimplifyContext context)
        {
            if (Name != null && !context.MakeDeclarationsGlobal &&
                !context.DefineIdentifier(Name, true))
            {
                throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, Name);
            }

            _functionScope = context.PushFunctionScope();

            for (var i = 0; i < Arguments.Count; i++)
            {
                var name = Arguments[i];

                if (!_functionScope.DefineArgument(i, name))
                {
                    throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);
                }
            }

            if (OtherArguments != null && !_functionScope.DefineArgument(Arguments.Count, OtherArguments))
            {
                throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, OtherArguments);
            }

            SimplifyBody(context);

            context.PopScope();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

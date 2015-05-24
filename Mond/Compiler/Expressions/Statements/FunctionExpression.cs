using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class FunctionExpression : Expression, IStatementExpression
    {
        public string Name { get; private set; }
        public ReadOnlyCollection<string> Arguments { get; private set; }
        public string OtherArguments { get; private set; }
        public bool IsOperator { get; private set; }
        public ScopeExpression Block { get; private set; }

        public string DebugName { get; set; }

        public bool HasChildren { get { return false; } }

        public FunctionExpression(Token token, string name, List<string> arguments, string otherArgs, bool isOperator, ScopeExpression block, string debugName = null)
            : base(token)
        {
            Name = name;
            Arguments = arguments.AsReadOnly();
            OtherArguments = otherArgs;
            IsOperator = isOperator;
            Block = block;

            DebugName = debugName;
        }

        public virtual void CompileBody(FunctionContext context)
        {
            var stack = 0;

            stack += context.Bind(context.Label);
            stack += context.Enter();

            if (OtherArguments != null)
                stack += context.VarArgs(Arguments.Count);

            stack += Block.Compile(context);
            stack += context.LoadUndefined();
            stack += context.Return();

            CheckStack(stack, 0);
        }

        public override int Compile(FunctionContext context)
        {
            if (IsOperator && Lexer.IsOperatorToken(Name) && !(Parent is BinaryOperatorExpression && Parent.Parent is BlockExpression && !(Parent.Parent is ScopeExpression)))
                throw new MondCompilerException(Token, CompilerError.CantNestOperatorDecl);

            var isStatement = Parent is IBlockExpression;
            var shouldBeGlobal = context.ArgIndex == 0 && context.Compiler.Options.MakeRootDeclarationsGlobal;
            var shouldStore = Name != null && !IsOperator;

            IdentifierOperand identifier = null;

            if (shouldStore && !shouldBeGlobal)
            {
                if (!context.DefineIdentifier(Name, true))
                    throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, Name);

                identifier = context.Identifier(Name);
            }

            // compile body
            var functionContext = context.MakeFunction(Name ?? DebugName);
            functionContext.Function(functionContext.FullName);
            functionContext.Position(Token);
            functionContext.PushScope();

            for (var i = 0; i < Arguments.Count; i++)
            {
                var name = Arguments[i];

                if (!functionContext.DefineArgument(i, name))
                    throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);
            }

            if (OtherArguments != null && !functionContext.DefineArgument(Arguments.Count, OtherArguments))
                throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, OtherArguments);

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

        public override Expression Simplify()
        {
            Block = (ScopeExpression)Block.Simplify();

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

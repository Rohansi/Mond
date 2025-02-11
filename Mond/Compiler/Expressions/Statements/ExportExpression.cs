using System;

namespace Mond.Compiler.Expressions.Statements
{
    internal class ExportExpression : Expression, IStatementExpression, IBlockExpression
    {
        public Expression DeclarationExpression { get; private set; }

        public bool HasChildren => true;

        public ExportExpression(Token token, Expression declarationExpression)
            : base(token)
        {
            if (declarationExpression == null)
                throw new ArgumentNullException(nameof(declarationExpression));
            if (declarationExpression is not IDeclarationExpression)
                throw new ArgumentException($"Declaration expression must implement {nameof(IDeclarationExpression)}", nameof(declarationExpression));

            DeclarationExpression = declarationExpression;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = DeclarationExpression.Compile(context);
            CheckStack(stack, 0);

            if (!context.TryGetIdentifier("exports", out var exportsOperand))
            {
                throw new MondCompilerException(this, CompilerError.ExportCannotBeUsedOutsideModule);
            }

            if (exportsOperand.FrameIndex >= 0 || exportsOperand.FrameIndex != -context.ArgIndex)
            {
                throw new MondCompilerException(this, CompilerError.ExportCannotBeUsedOutsideModule);
            }

            if (Parent is not ScopeExpression { Parent: FunctionExpression moduleFunction } ||
                moduleFunction.Arguments.Count != 1 || moduleFunction.Arguments[0] != "exports")
            {
                throw new MondCompilerException(this, CompilerError.ExportOnlyOnTopLevelDeclarations);
            }

            var declaration = (IDeclarationExpression)DeclarationExpression;
            foreach (var identifier in declaration.DeclaredIdentifiers)
            {
                stack += context.Load(context.Identifier(identifier));
                stack += context.Load(exportsOperand);
                stack += context.StoreField(context.String(identifier));
            }

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            DeclarationExpression = DeclarationExpression.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            DeclarationExpression.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

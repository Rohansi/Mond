using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    class VarExpression : Expression, IStatementExpression
    {
        public class Declaration
        {
            public string Name { get; }
            public Expression Initializer { get; }

            public Declaration(string name, Expression initializer)
            {
                Name = name;
                Initializer = initializer;
            }
        }

        public ReadOnlyCollection<Declaration> Declarations { get; private set; }
        public bool IsReadOnly { get; }

        public bool HasChildren => true;

        public VarExpression(Token token, List<Declaration> declarations, bool isReadOnly = false)
            : base(token)
        {
            Declarations = declarations.AsReadOnly();
            IsReadOnly = isReadOnly;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var isSingleInitialized = Declarations.Count == 1 && Declarations[0].Initializer != null;
            if (isSingleInitialized)
                context.Statement(this);

            var stack = 0;
            var shouldBeGlobal = context.ArgIndex == 0 && context.Compiler.Options.MakeRootDeclarationsGlobal;

            foreach (var declaration in Declarations)
            {
                var name = declaration.Name;

                if (!shouldBeGlobal)
                {
                    if (!context.DefineIdentifier(name, IsReadOnly))
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);
                }

                if (declaration.Initializer == null)
                    continue;

                if (!isSingleInitialized)
                    context.Statement(declaration.Initializer);

                if (!shouldBeGlobal)
                {
                    var identifier = context.Identifier(name);

                    stack += declaration.Initializer.Compile(context);
                    stack += context.Store(identifier);
                }
                else
                {
                    stack += declaration.Initializer.Compile(context);
                    stack += context.LoadGlobal();
                    stack += context.StoreField(context.String(name));
                }
            }

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Declarations = Declarations
                .Select(d => new Declaration(d.Name, d.Initializer?.Simplify()))
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var declaration in Declarations.Where(d => d.Initializer != null))
            {
                declaration.Initializer.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

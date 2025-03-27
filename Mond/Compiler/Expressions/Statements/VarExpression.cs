using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    internal class VarExpression : Expression, IStatementExpression, IDeclarationExpression
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
        public IEnumerable<string> DeclaredIdentifiers => Declarations.Select(d => d.Name);

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
            var shouldBeGlobal = context.MakeDeclarationsGlobal;

            foreach (var declaration in Declarations)
            {
                var name = declaration.Name;
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

        public override Expression Simplify(SimplifyContext context)
        {
            if (!context.MakeDeclarationsGlobal)
            {
                foreach (var declaration in Declarations)
                {
                    if (!context.DefineIdentifier(declaration.Name, IsReadOnly))
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, declaration.Name);
                }
            }

            Declarations = Declarations
                .Select(d => new Declaration(d.Name, d.Initializer?.Simplify(context)))
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

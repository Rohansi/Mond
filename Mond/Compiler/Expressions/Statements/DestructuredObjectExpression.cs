using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class DestructuredObjectExpression : Expression, IStatementExpression
    {
        public class Field
        {
            public string Name { get; }
            public string Alias { get; }

            public Field(string name, string alias)
            {
                Name = name;
                Alias = alias;
            }
        }

        public ReadOnlyCollection<Field> Fields { get; }
        public Expression Initializer { get; private set; }
        public bool IsReadOnly { get; }
        public bool HasChildren => false;

        public DestructuredObjectExpression(Token token, IList<Field> fields, Expression initializer, bool isReadOnly)
            : base(token)
        {
            Fields = new ReadOnlyCollection<Field>(fields);
            Initializer = initializer;
            IsReadOnly = isReadOnly;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = Initializer?.Compile(context) ?? 1;
            var global = context.MakeDeclarationsGlobal;

            foreach (var field in Fields)
            {
                var name = field.Alias ?? field.Name;

                stack += context.Dup();
                stack += context.LoadField(context.String(field.Name));
                
                if (global)
                {
                    stack += context.LoadGlobal();
                    stack += context.StoreField(context.String(name));
                }
                else
                {
                    stack += context.Store(context.Identifier(name));
                }
            }

            stack += context.Drop();
            
            CheckStack(stack, 0);
            return -1;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Initializer = Initializer?.Simplify(context);

            if (!context.MakeDeclarationsGlobal)
            {
                foreach (var field in Fields)
                {
                    var name = field.Alias ?? field.Name;
                    if (!context.DefineIdentifier(name, IsReadOnly))
                    {
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);
                    }
                }
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Initializer?.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class DestructuredObjectExpression : Expression, IStatementExpression
    {
        public class Field
        {
            public string Name { get; private set; }
            public string Alias { get; private set; }

            public Field(string name, string alias)
            {
                Name = name;
                Alias = alias;
            }
        }

        public ReadOnlyCollection<Field> Fields { get; private set; }
        public Expression Initializer { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool HasChildren { get { return false; } }

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

            var stack = Initializer == null ? 1 : Initializer.Compile(context);
            var global = context.ArgIndex == 0 && context.Compiler.Options.MakeRootDeclarationsGlobal;

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
                    if (!context.DefineIdentifier(name, IsReadOnly))
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);

                    stack += context.Store(context.Identifier(name));
                }
            }

            stack += context.Drop();
            
            CheckStack(stack, 0);
            return -1;
        }

        public override Expression Simplify()
        {
            if (Initializer != null)
                Initializer = Initializer.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Initializer != null)
                Initializer.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

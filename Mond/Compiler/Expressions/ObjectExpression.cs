using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class ObjectExpression : Expression
    {
        public ReadOnlyCollection<KeyValuePair<string, Expression>> Values { get; private set; }
         
        public ObjectExpression(Token token, List<KeyValuePair<string, Expression>> values)
            : base(token)
        {
            Values = values.AsReadOnly();
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;

            stack += context.NewObject();

            foreach (var value in Values)
            {
                stack += context.Dup();
                stack += value.Value.Compile(context);
                stack += context.Swap();
                stack += context.StoreField(context.String(value.Key));
            }

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Values = Values
                .Select(v => new KeyValuePair<string, Expression>(v.Key, v.Value.Simplify(context)))
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var value in Values)
            {
                value.Value.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

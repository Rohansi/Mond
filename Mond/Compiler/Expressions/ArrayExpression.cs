using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class ArrayExpression : Expression
    {
        public ReadOnlyCollection<Expression> Values { get; private set; }
         
        public ArrayExpression(Token token, List<Expression> values)
            : base(token)
        {
            Values = values.AsReadOnly();
        }

        public override int Compile(FunctionContext context)
        {
            var stack = Values.Sum(value => value.Compile(context));

            context.Position(Token); // debug info
            stack += context.NewArray(Values.Count);

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify()
        {
            Values = Values
                .Select(e => e.Simplify())
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var value in Values)
            {
                value.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

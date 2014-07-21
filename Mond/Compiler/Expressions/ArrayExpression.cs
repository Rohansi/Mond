using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class ArrayExpression : Expression
    {
        public ReadOnlyCollection<Expression> Values { get; private set; }
         
        public ArrayExpression(Token token, List<Expression> values)
            : base(token.FileName, token.Line)
        {
            Values = values.AsReadOnly();
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Array");

            writer.Indent++;
            foreach (var value in Values)
            {
                value.Print(writer);
            }
            writer.Indent--;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Values.Sum(value => value.Compile(context));
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
    }
}

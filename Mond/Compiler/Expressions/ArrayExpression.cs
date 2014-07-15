using System;
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

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Array");

            foreach (var value in Values)
            {
                value.Print(indent + 1);
            }
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            foreach (var value in Values)
            {
                CompileCheck(context, value, 1);
            }

            context.NewArray(Values.Count);
            return 1;
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

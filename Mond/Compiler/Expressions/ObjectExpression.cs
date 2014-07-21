using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class ObjectExpression : Expression
    {
        public ReadOnlyCollection<KeyValuePair<string, Expression>> Values { get; private set; }
         
        public ObjectExpression(Token token, List<KeyValuePair<string, Expression>> values)
            : base(token.FileName, token.Line)
        {
            Values = values.AsReadOnly();
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Object");

            foreach (var value in Values)
            {
                writer.WriteIndent();
                writer.WriteLine("-" + value.Key);

                writer.Indent += 2;
                value.Value.Print(writer);
                writer.Indent -= 2;
            }
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

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

        public override Expression Simplify()
        {
            Values = Values
                .Select(v => new KeyValuePair<string, Expression>(v.Key, v.Value.Simplify()))
                .ToList()
                .AsReadOnly();

            return this;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class CallExpression : Expression
    {
        public Expression Method { get; private set; }
        public ReadOnlyCollection<Expression> Arguments { get; private set; }

        public CallExpression(Token token, Expression method, List<Expression> arguments)
            : base(token.FileName, token.Line)
        {
            Method = method;
            Arguments = arguments.AsReadOnly();
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Call");

            Console.Write(indentStr);
            Console.WriteLine("-Expression");

            Method.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-Arguments");

            foreach (var arg in Arguments)
            {
                arg.Print(indent + 2);
            }
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            // load "this" value
            var field = Method as FieldExpression;
            if (field != null)
            {
                CompileCheck(context, field.Left, 1);
            }
            else
            {
                context.LoadUndefined();
            }

            foreach (var argument in Arguments)
            {
                CompileCheck(context, argument, 1);
            }

            CompileCheck(context, Method, 1);
            context.Call(Arguments.Count + 1);

            return 1;
        }

        public override Expression Simplify()
        {
            Method = Method.Simplify();

            Arguments = Arguments
                .Select(a => a.Simplify())
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Method.SetParent(this);

            foreach (var arg in Arguments)
            {
                arg.SetParent(this);
            }
        }
    }
}

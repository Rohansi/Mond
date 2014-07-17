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

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Arguments.Sum(argument => argument.Compile(context));

            stack += Method.Compile(context);
            stack += context.Call(Arguments.Count);

            CheckStack(stack, 1);
            return stack;
        }

        public int CompileTailCall(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Arguments.Sum(argument => argument.Compile(context));
            stack += context.TailCall(Arguments.Count, context.Label);

            CheckStack(stack, 0);
            return stack;
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

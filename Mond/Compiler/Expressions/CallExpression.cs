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

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Call");

            writer.WriteIndent();
            writer.WriteLine("-Expression");

            writer.Indent += 2;
            Method.Print(writer);
            writer.Indent -= 2;

            writer.WriteIndent();
            writer.WriteLine("-Arguments");

            writer.Indent += 2;
            foreach (var arg in Arguments)
            {
                arg.Print(writer);
            }
            writer.Indent -= 2;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Arguments.Sum(argument => argument.Compile(context));

            stack += Method.Compile(context);
            stack += context.Call(Arguments.Count, GetUnpackIndices());

            CheckStack(stack, 1);
            return stack;
        }

        public int CompileTailCall(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Arguments.Sum(argument => argument.Compile(context));
            stack += context.TailCall(Arguments.Count, context.Label, GetUnpackIndices());

            CheckStack(stack, 0);
            return stack;
        }

        private List<ImmediateOperand> GetUnpackIndices()
        {
            var unpackIndices = Arguments
                .Select((e, i) => new { Expression = e, Index = i })
                .Where(e => e.Expression is UnpackExpression)
                .Select(e => new ImmediateOperand(e.Index))
                .OrderByDescending(i => i.Value)
                .ToList();

            if (unpackIndices.Count < byte.MinValue || unpackIndices.Count > byte.MaxValue)
                throw new MondCompilerException(FileName, Line, CompilerError.TooManyUnpacks);

            return unpackIndices;
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

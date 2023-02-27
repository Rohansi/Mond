using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class CallExpression : Expression
    {
        public Expression Method { get; private set; }
        public ReadOnlyCollection<Expression> Arguments { get; private set; }

        public override Token StartToken => Method.StartToken;

        public CallExpression(Token token, Expression method, List<Expression> arguments)
            : base(token)
        {
            Method = method;
            Arguments = arguments.AsReadOnly();
        }

        public override int Compile(FunctionContext context)
        {
            if (Method is FieldExpression field)
            {
                var methodIdent = context.DefineInternal("method", true);

                // compile as a 'this' call using uniform function call syntax: o.f(a, b) is equivalent to f(o, a, b)
                var stack = field.CompilePreLoadStore(context, 2); // o, o]
                stack += field.CompileLoad(context); // o, f]
                stack += context.Store(methodIdent); // o]
                
                stack += Arguments.Sum(argument => argument.Compile(context)); // f, o, a, b]

                stack += context.Load(methodIdent);

                context.Position(Token); // debug info
                stack += context.Call(Arguments.Count + 1, GetUnpackIndices(1));

                CheckStack(stack, 1);
                return stack;
            }
            else
            {
                var stack = Arguments.Sum(argument => argument.Compile(context));

                stack += Method.Compile(context);

                context.Position(Token); // debug info
                stack += context.Call(Arguments.Count, GetUnpackIndices(0));

                CheckStack(stack, 1);
                return stack;
            }
        }

        public int CompileTailCall(FunctionContext context)
        {
            var stack = Arguments.Sum(argument => argument.Compile(context));

            context.Position(Token); // debug info
            stack += context.TailCall(Arguments.Count, context.Label, GetUnpackIndices(0));

            CheckStack(stack, 0);
            return stack;
        }

        private List<ImmediateOperand> GetUnpackIndices(int offset)
        {
            var unpackIndices = Arguments
                .Select((e, i) => new { Expression = e, Index = offset + i })
                .Where(e => e.Expression is UnpackExpression)
                .Select(e => new ImmediateOperand(e.Index))
                .OrderByDescending(i => i.Value)
                .ToList();

            if (unpackIndices.Count < byte.MinValue || unpackIndices.Count > byte.MaxValue)
                throw new MondCompilerException(this, CompilerError.TooManyUnpacks);

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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

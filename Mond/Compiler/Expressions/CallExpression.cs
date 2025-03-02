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
            var stack = 0;

            if (Method is FieldExpression methodField &&
                methodField.Left is not GlobalExpression &&
                !(methodField.Left is FieldExpression instanceField && IgnoreInstanceCall(instanceField.Name)) &&
                !(methodField.Left is IdentifierExpression instanceIdent && IgnoreInstanceCall(instanceIdent.Name)))
            {
                stack += methodField.Left.Compile(context);
                stack += Arguments.Sum(argument => argument.Compile(context));

                context.Position(Token); // debug info
                stack += context.InstanceCall(context.String(methodField.Name), Arguments.Count, GetUnpackIndices());
            }
            else
            {
                stack += Method.Compile(context);
                stack += Arguments.Sum(argument => argument.Compile(context));

                context.Position(Token); // debug info
                stack += context.Call(Arguments.Count, GetUnpackIndices());
            }

            CheckStack(stack, 1);
            return stack;
        }

        private static bool IgnoreInstanceCall(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]);
        }

        public int CompileTailCall(FunctionContext context)
        {
            var stack = Arguments.Sum(argument => argument.Compile(context));

            context.Position(Token); // debug info
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
                throw new MondCompilerException(this, CompilerError.TooManyUnpacks);

            return unpackIndices;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Method = Method.Simplify(context);

            Arguments = Arguments
                .Select(a => a.Simplify(context))
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

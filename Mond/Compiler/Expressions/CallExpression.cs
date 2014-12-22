﻿using System.Collections.Generic;
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

        public override int Compile(FunctionContext context)
        {
            var stack = Arguments.Sum(argument => argument.Compile(context));

            stack += Method.Compile(context);

            context.Line(FileName, Line); // debug info
            stack += context.Call(Arguments.Count, GetUnpackIndices());

            CheckStack(stack, 1);
            return stack;
        }

        public int CompileTailCall(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = Arguments.Sum(argument => argument.Compile(context));

            context.Line(FileName, Line); // debug info
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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

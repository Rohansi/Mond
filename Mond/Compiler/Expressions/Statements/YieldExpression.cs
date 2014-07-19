using System;

namespace Mond.Compiler.Expressions.Statements
{
    class YieldExpression : Expression, IStatementExpression
    {
        public Expression Value { get; private set; }

        public YieldExpression(Token token, Expression value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Yield");

            Value.Print(indent + 1);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var sequenceContext = context as SequenceBodyContext;
            if (sequenceContext == null)
                throw new MondCompilerException(FileName, Line, CompilerError.YieldInFun);

            var state = sequenceContext.SequenceBody.State;
            var enumerable = sequenceContext.SequenceBody.Enumerable;

            var stack = 0;
            var nextState = sequenceContext.SequenceBody.NextState;
            var nextStateLabel = sequenceContext.SequenceBody.MakeStateLabel(context);

            stack += context.Load(context.Number(nextState));
            stack += context.Store(state);

            stack += Value.Compile(context);
            stack += context.Load(enumerable);
            stack += context.StoreField(context.String("current"));

            stack += context.LoadTrue();
            stack += context.Return();

            stack += context.Bind(nextStateLabel);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Value = Value.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Value.SetParent(this);
        }
    }
}

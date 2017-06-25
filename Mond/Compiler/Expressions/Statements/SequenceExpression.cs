using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    class SequenceExpression : FunctionExpression
    {
        public SequenceExpression(Token token, string name, List<string> arguments, string otherArgs, ScopeExpression block, string debugName = null)
            : base(token, name, arguments, otherArgs, block, debugName)
        {

        }

        public override void CompileBody(FunctionContext context)
        {
            var state = context.DefineInternal("state");
            var enumerable = context.DefineInternal("enumerable");

            var stack = 0;
            var bodyToken = new Token(Token, TokenType.Fun, null);
            var body = new SequenceBodyExpression(bodyToken, null, Block, "moveNext", state, enumerable);
            var seqContext = new SequenceContext(context.Compiler, "moveNext", body, context);

            var getEnumerator = context.MakeFunction("getEnumerator");
            getEnumerator.Function(getEnumerator.FullName);
            getEnumerator.Bind(getEnumerator.Label);
            getEnumerator.Enter();
            getEnumerator.Load(enumerable);
            getEnumerator.Return();

            var dispose = context.MakeFunction("dispose");
            dispose.Function(dispose.FullName);
            dispose.Bind(dispose.Label);
            dispose.Enter();
            dispose.LoadUndefined();
            dispose.Return();

            stack += context.Bind(context.Label);
            stack += context.Enter();

            if (OtherArguments != null)
                stack += context.VarArgs(Arguments.Count);

            stack += context.Load(context.Number(0));
            stack += context.Store(state);

            stack += context.NewObject();

            stack += context.Dup();
            stack += context.LoadUndefined();
            stack += context.Swap();
            stack += context.StoreField(context.String("current"));

            stack += context.Dup();
            stack += body.Compile(seqContext);
            stack += context.Swap();
            stack += context.StoreField(context.String("moveNext"));

            stack += context.Dup();
            stack += context.Closure(getEnumerator.Label);
            stack += context.Swap();
            stack += context.StoreField(context.String("getEnumerator"));

            stack += context.Dup();
            stack += context.Closure(dispose.Label);
            stack += context.Swap();
            stack += context.StoreField(context.String("dispose"));

            stack += context.Store(enumerable);

            stack += context.Load(enumerable);
            stack += context.Return();

            CheckStack(stack, 0);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    class SequenceBodyExpression : FunctionExpression
    {
        private readonly List<LabelOperand> _stateLabels;

        public IdentifierOperand State { get; }
        public IdentifierOperand Enumerable { get; }

        public int NextState => _stateLabels.Count;
        public LabelOperand EndLabel { get; private set; }

        public SequenceBodyExpression(Token token, string name, ScopeExpression block, string debugName,
                                      IdentifierOperand state, IdentifierOperand enumerable)
            : base(token, name, new List<string> { "#input" }, null, block, debugName)
        {
            _stateLabels = new List<LabelOperand>();

            State = state;
            Enumerable = enumerable;
        }

        public LabelOperand MakeStateLabel(FunctionContext context)
        {
            var label = context.MakeLabel(string.Format("state_{0}", NextState));
            _stateLabels.Add(label);
            return label;
        }

        public override void CompileBody(FunctionContext context)
        {
            var stack = 0;

            EndLabel = context.MakeLabel("state_end");

            stack += context.Bind(context.Label);
            stack += context.Enter();

            // jump to state label
            stack += context.Load(State);
            stack += context.JumpTable(0, _stateLabels);
            stack += context.Jump(EndLabel);

            // first state
            stack += context.Bind(MakeStateLabel(context));

            // compile body
            stack += Block.Compile(context);

            // set enumerator.current to undefined
            // do this before EndLabel so we dont overwrite return values
            stack += context.LoadUndefined();
            stack += context.Load(Enumerable);
            stack += context.StoreField(context.String("current"));

            stack += context.Bind(EndLabel);

            // set state to end
            stack += context.Load(context.Number(-1));
            stack += context.Store(State);

            stack += context.LoadFalse();
            stack += context.Return();

            CheckStack(stack, 0);
        }
    }

    class SequenceContext : FunctionContext
    {
        private readonly SequenceBodyExpression _sequenceBody;
        private readonly FunctionContext _forward;

        public SequenceContext(ExpressionCompiler compiler, string name, SequenceBodyExpression sequenceBody, FunctionContext forward)
            : base(compiler, forward.ArgIndex, forward.LocalIndex, forward.Scope, forward.FullName, name)
        {
            _sequenceBody = sequenceBody;
            _forward = forward;
        }

        public override FunctionContext MakeFunction(string name)
        {
            var context = new SequenceBodyContext(Compiler, name, _forward, _sequenceBody);
            Compiler.RegisterFunction(context);
            return context;
        }

        public override void Emit(Instruction instruction)
        {
            _forward.Emit(instruction);
        }
    }

    class SequenceBodyContext : FunctionContext
    {
        public SequenceBodyExpression SequenceBody { get; }

        public SequenceBodyContext(ExpressionCompiler compiler, string name, FunctionContext parent, SequenceBodyExpression sequenceBody)
            : base(compiler, parent.ArgIndex + 1, parent.LocalIndex + 1, parent.Scope, parent.FullName, name)
        {
            SequenceBody = sequenceBody;
        }
    }
}

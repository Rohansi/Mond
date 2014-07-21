using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    class SequenceExpression : FunctionExpression
    {
        public SequenceExpression(Token token, string name, List<string> arguments, string otherArgs, BlockExpression block)
            : base(token, name, arguments, otherArgs, block)
        {

        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Sequence " + Name);

            writer.WriteIndent();
            writer.WriteLine("-Arguments: {0}", string.Join(", ", Arguments));

            if (OtherArguments != null)
            {
                writer.WriteIndent();
                writer.WriteLine("-Other Arguments: {0}", OtherArguments);
            }

            writer.WriteIndent();
            writer.WriteLine("-Block");

            writer.Indent += 2;
            Block.Print(writer);
            writer.Indent -= 2;
        }

        public override void CompileBody(FunctionContext context)
        {
            var state = context.DefineInternal("state");
            var enumerable = context.DefineInternal("enumerable");

            var stack = 0;
            var bodyToken = new Token(FileName, Line, TokenType.Fun, null);
            var body = new SequenceBodyExpression(bodyToken, null, Block, "moveNext", state, enumerable);
            var seqContext = new SequenceContext(context.Compiler, "moveNext", body, context);

            var getEnumerator = context.MakeFunction("getEnumerator");
            getEnumerator.Function(getEnumerator.FullName);
            getEnumerator.Bind(getEnumerator.Label);
            getEnumerator.Enter();
            getEnumerator.Load(enumerable);
            getEnumerator.Return();

            stack += context.Bind(context.Label);
            stack += context.Enter();

            if (OtherArguments != null)
                stack += context.VarArgs(Arguments.Count);

            stack += context.Load(context.Number(0));
            stack += context.Store(state);

            stack += context.NewObject();

            stack += context.Dup();
            stack += context.LoadNull();
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

            stack += context.Store(enumerable);

            stack += context.Load(enumerable);
            stack += context.Return();

            CheckStack(stack, 0);
        }
    }

    class SequenceBodyExpression : FunctionExpression
    {
        private List<LabelOperand> _stateLabels;

        public readonly IdentifierOperand State;
        public readonly IdentifierOperand Enumerable;

        public int NextState { get { return _stateLabels.Count; } }
        public LabelOperand EndLabel { get; private set; }

        public SequenceBodyExpression(Token token, string name, BlockExpression block, string debugName,
                                      IdentifierOperand state, IdentifierOperand enumerable)
            : base(token, name, new List<string>(), null, block, debugName)
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

            stack += context.Bind(EndLabel);

            // set state to end
            stack += context.Load(context.Number(-1));
            stack += context.Store(State);

            // set enumerator.current to null
            stack += context.LoadNull();
            stack += context.Load(Enumerable);
            stack += context.StoreField(context.String("current"));

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
            : base(compiler, forward.FrameIndex, forward.Scope, forward.FullName, name)
        {
            _sequenceBody = sequenceBody;
            _forward = forward;
        }

        public override FunctionContext MakeFunction(string name)
        {
            var context = new SequenceBodyContext(Compiler, name, _sequenceBody, _forward);
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
        private readonly FunctionContext _forward;

        public readonly SequenceBodyExpression SequenceBody;

        public SequenceBodyContext(ExpressionCompiler compiler, string name, SequenceBodyExpression sequenceBody, FunctionContext forward)
            : base(compiler, forward.FrameIndex + 1, forward.Scope, forward.FullName, name)
        {
            _forward = forward;

            SequenceBody = sequenceBody;
        }

        public override bool DefineIdentifier(string name, bool isReadOnly = false)
        {
            return _forward.DefineIdentifier(name, isReadOnly);
        }

        public override IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            return _forward.DefineInternal(name, canHaveMultiple);
        }
    }
}

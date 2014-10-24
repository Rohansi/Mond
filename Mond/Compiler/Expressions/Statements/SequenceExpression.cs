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

            stack += context.Dup();
            stack += context.Closure(dispose.Label);
            stack += context.Swap();
            stack += context.StoreField(context.String("dispose"));

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
        class IdentifierMap
        {
            public readonly IdentifierMap Previous;
            private Dictionary<string, string> _identifiers;

            public IdentifierMap(IdentifierMap previous = null)
            {
                Previous = previous;
                _identifiers = new Dictionary<string, string>();
            }

            public bool Add(string name, string value)
            {
                if (Get(name) != null)
                    return false;

                _identifiers.Add(name, value);
                return true;
            }

            public string Get(string name)
            {
                var curr = this;

                do
                {
                    string value;
                    if (curr._identifiers.TryGetValue(name, out value))
                        return value;

                    curr = curr.Previous;
                } while (curr != null);

                return null;
            }
        }

        private readonly FunctionContext _forward;
        private IdentifierMap _identifierMap;
        private int _index;

        public readonly SequenceBodyExpression SequenceBody;

        public SequenceBodyContext(ExpressionCompiler compiler, string name, SequenceBodyExpression sequenceBody, FunctionContext forward)
            : base(compiler, forward.FrameIndex + 1, forward.Scope, forward.FullName, name)
        {
            _forward = forward;
            _identifierMap = new IdentifierMap();
            _index = 0;

            SequenceBody = sequenceBody;
        }

        public override bool DefineIdentifier(string name, bool isReadOnly = false)
        {
            var uniqueName = string.Format("{0}#{1}", name, _index);
            if (!_identifierMap.Add(name, uniqueName))
                return false;

            _index++;

            return _forward.DefineIdentifier(uniqueName, isReadOnly);
        }

        public string Get(string name)
        {
            return _identifierMap.Get(name);
        }

        public override void PushScope()
        {
            _identifierMap = new IdentifierMap(_identifierMap);
            Scope = new SequenceBodyScope(FrameIndex, Scope, this);
        }

        public override void PopScope()
        {
            _identifierMap = _identifierMap.Previous;
            Scope = Scope.Previous;
        }

        public override IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            return _forward.DefineInternal(name, canHaveMultiple);
        }
    }

    class SequenceBodyScope : Scope
    {
        private readonly SequenceBodyContext _context;

        public SequenceBodyScope(int frameIndex, Scope previous, SequenceBodyContext context)
            : base(frameIndex, previous)
        {
            _context = context;
        }

        public override IdentifierOperand Get(string name, bool inherit = true)
        {
            var uniqueName = _context.Get(name);
            if (uniqueName != null)
                name = uniqueName;

            return base.Get(name, inherit);
        }
    }
}

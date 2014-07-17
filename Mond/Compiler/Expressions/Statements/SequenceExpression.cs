using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    class SequenceExpression : FunctionExpression
    {
        public SequenceExpression(Token token, string name, List<string> arguments, BlockExpression block)
            : base(token, name, arguments, block)
        {

        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Sequence " + Name);

            Console.Write(indentStr);
            Console.Write("-Arguments: ");
            Console.WriteLine(string.Join(", ", Arguments));

            Console.Write(indentStr);
            Console.WriteLine("-Block");
            Block.Print(indent + 2);
        }

        public override void CompileBody(FunctionContext context)
        {
            if (!context.DefineIdentifier("#state", false, true) || !context.DefineIdentifier("#enumerator", false, true))
                throw new MondCompilerException(FileName, Line, CompilerError.FailedToDefineInternal);

            var stack = 0;
            var state = context.Identifier("#state");
            var enumerator = context.Identifier("#enumerator");
            var body = new SequenceBodyExpression(new Token(FileName, Line, TokenType.Fun, null), null, new List<string>(), Block);
            var seqContext = new SequenceContext(context.Compiler, "sequence", body, context);

            stack += context.Bind(context.Label);
            stack += context.Enter();

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

            stack += context.Store(enumerator);

            stack += context.Load(enumerator);
            stack += context.Return();

            CheckStack(stack, 0);
        }
    }

    class SequenceBodyExpression : FunctionExpression
    {
        private List<LabelOperand> _stateLabels;

        public int NextState { get { return _stateLabels.Count; } }
        public LabelOperand EndLabel { get; private set; }

        public SequenceBodyExpression(Token token, string name, List<string> arguments, BlockExpression block)
            : base(token, name, arguments, block)
        {
            _stateLabels = new List<LabelOperand>();
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
            var state = context.Identifier("#state");
            var enumerator = context.Identifier("#enumerator");
            EndLabel = context.MakeLabel("state_end");

            stack += context.Bind(context.Label);
            stack += context.Enter();

            // jump to state label
            stack += context.Load(state);
            stack += context.JumpTable(0, _stateLabels);
            stack += context.Jump(EndLabel);

            // first state
            stack += context.Bind(MakeStateLabel(context));

            // compile body
            stack += Block.Compile(context);

            stack += context.Bind(EndLabel);

            // set state to end
            stack += context.Load(context.Number(-1));
            stack += context.Store(state);

            // set enumerator.current to null
            stack += context.LoadNull();
            stack += context.Load(enumerator);
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
            : base(compiler, name)
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
            : base(compiler, name)
        {
            _forward = forward;

            SequenceBody = sequenceBody;
        }

        public override void PushFrame()
        {
            base.PushScope();
        }

        public override void PopFrame()
        {
            base.PopScope();
        }

        public override bool DefineIdentifier(string name, bool isReadOnly = false, bool allowOverlap = false)
        {
            return _forward.DefineIdentifier(name, isReadOnly, allowOverlap);
        }
    }
}

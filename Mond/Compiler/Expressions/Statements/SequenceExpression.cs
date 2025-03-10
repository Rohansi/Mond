using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    internal class SequenceExpression : FunctionExpression
    {
        private readonly SequenceBodyExpression _body;

        private IdentifierOperand _state;
        private IdentifierOperand _enumerable;
        private Scope _getEnumeratorScope;
        private Scope _disposeScope;

        public SequenceExpression(Token token, string name, List<string> arguments, string otherArgs, ScopeExpression block, string debugName = null)
            : base(token, name, arguments, otherArgs, block, debugName)
        {
            var bodyToken = new Token(Token, TokenType.Fun, null);
            _body = new SequenceBodyExpression(bodyToken, null, Block, "moveNext");
        }

        public override void CompileBody(FunctionContext context)
        {
            var getEnumerator = context.MakeFunction("getEnumerator", _getEnumeratorScope);
            getEnumerator.Load(_enumerable);
            getEnumerator.Return();

            var dispose = context.MakeFunction("dispose", _disposeScope);
            dispose.LoadUndefined();
            dispose.Return();

            var stack = 0;

            if (OtherArguments != null)
                stack += context.VarArgs(Arguments.Count);

            stack += context.Load(context.Number(0));
            stack += context.Store(_state);

            stack += context.NewObject();

            stack += context.Dup();
            stack += context.LoadUndefined();
            stack += context.Swap();
            stack += context.StoreField(context.String("current"));

            stack += context.Dup();
            var seqContext = new SequenceContext(context.Compiler, "moveNext", _body, context);
            stack += _body.Compile(seqContext);
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

            stack += context.Store(_enumerable);

            stack += context.Load(_enumerable);
            stack += context.Return();

            CheckStack(stack, 0);
        }

        protected override void SimplifyBody(SimplifyContext context)
        {
            _state = context.DefineInternal("state");
            _enumerable = context.DefineInternal("enumerable");

            _getEnumeratorScope = context.PushFunctionScope();
            context.PopScope();

            _disposeScope = context.PushFunctionScope();
            context.PopScope();

            _body.State = _state;
            _body.Enumerable = _enumerable;
            _body.Simplify(context);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class SequenceBodyExpression : FunctionExpression
    {
        private readonly List<LabelOperand> _stateLabels;

        public IdentifierOperand State { get; set; }
        public IdentifierOperand Enumerable { get; set; }

        public int NextState => _stateLabels.Count;
        public LabelOperand EndLabel { get; private set; }

        public SequenceBodyExpression(Token token, string name, ScopeExpression block, string debugName)
            : base(token, name, ["#instance", "#input"], null, block, debugName)
        {
            _stateLabels = new List<LabelOperand>();
        }

        public LabelOperand MakeStateLabel(FunctionContext context)
        {
            var label = context.MakeLabel($"state_{NextState}");
            _stateLabels.Add(label);
            return label;
        }

        public override void CompileBody(FunctionContext context)
        {
            var stack = 0;

            EndLabel = context.MakeLabel("state_end");

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

    internal class SequenceContext : FunctionContext
    {
        private readonly SequenceBodyExpression _sequenceBody;
        private readonly FunctionContext _forward;

        public SequenceContext(ExpressionCompiler compiler, string name, SequenceBodyExpression sequenceBody, FunctionContext forward)
            : base(compiler, forward.Scope, forward.FullName, name)
        {
            _sequenceBody = sequenceBody;
            _forward = forward;
        }

        public override FunctionContext MakeFunction(string name, Scope scope)
        {
            if (scope.Previous != Scope)
            {
                throw new ArgumentException("Function scope must be linked to the current scope", nameof(scope));
            }

            var context = new SequenceBodyContext(Compiler, scope, name, _forward.FullName, _sequenceBody);
            Compiler.RegisterFunction(context);
            return context;
        }

        public override void Emit(Instruction instruction)
        {
            _forward.Emit(instruction);
        }
    }

    internal class SequenceBodyContext : FunctionContext
    {
        public SequenceBodyExpression SequenceBody { get; }

        public SequenceBodyContext(ExpressionCompiler compiler, Scope scope, string name, string parentName, SequenceBodyExpression sequenceBody)
            : base(compiler, scope, parentName, name)
        {
            SequenceBody = sequenceBody;
        }
    }
}

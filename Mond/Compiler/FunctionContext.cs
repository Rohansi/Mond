using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.Compiler
{
    internal partial class FunctionContext
    {
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public ExpressionCompiler Compiler { get; }

        public int Depth => Scope.Depth;
        public Scope Scope { get; protected set; }

        public string ParentName { get; }
        public string Name { get; }
        public string FullName { get; }

        public IdentifierOperand AssignedName { get; }
        public LabelOperand Label { get; }

        public bool MakeDeclarationsGlobal => Depth == 0 && Compiler.Options.MakeRootDeclarationsGlobal;

        public FunctionContext(ExpressionCompiler compiler, Scope scope, string parentName, string name)
        {
            _instructions = new List<Instruction>();
            _loopLabels = new IndexedStack<Tuple<LabelOperand, LabelOperand>>();

            Compiler = compiler;
            Scope = scope;

            ParentName = parentName;
            Name = name;
            FullName = $"{parentName}{(string.IsNullOrEmpty(parentName) ? "" : ".")}{Name ?? ""}";

            AssignedName = name != null ? scope.Get(name) : null;
            Label = Compiler.MakeLabel("function");
        }

        public virtual FunctionContext Root => this;

        public IEnumerable<Instruction> Instructions => _instructions;

        public virtual FunctionContext MakeFunction(string name, Scope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (scope.Previous != Scope)
            {
                throw new ArgumentException("Function scope must be linked to the current scope", nameof(scope));
            }

            if (scope.Depth != (Scope?.Depth ?? -1) + 1)
            {
                throw new ArgumentException("Function scope must have depth right above the current scope", nameof(scope));
            }

            name ??= $"lambda_{Compiler.LambdaId++}";

            var context = new FunctionContext(Compiler, scope, FullName, name);
            Compiler.RegisterFunction(context);
            return context;
        }

        public LabelOperand MakeLabel(string name = null)
        {
            return Compiler.MakeLabel(name);
        }

        public void PushScope(Scope scope)
        {
            if (scope.Previous != Scope)
            {
                throw new ArgumentException("Pushed scope must be linked to the current scope", nameof(scope));
            }

            Compiler.ScopeDepth++;

            if (Compiler.Options.DebugInfo <= MondDebugInfoLevel.StackTrace)
            {
                Scope = scope;
                return;
            }

            var startLabel = MakeLabel("scopeStart");
            var endLabel = MakeLabel("scopeEnd");

            scope.PopAction = () => Bind(endLabel);

            Emit(new Instruction(InstructionType.Scope, new IInstructionOperand[]
            {
                new ImmediateOperand(scope.Id),
                new ImmediateOperand(Compiler.ScopeDepth),
                new ImmediateOperand(Scope?.Id ?? -1),
                startLabel,
                endLabel,
                new DeferredOperand<ListOperand<DebugIdentifierOperand>>(() =>
                {
                    var operands = scope.Identifiers
                        .Select(i => new DebugIdentifierOperand(String(i.Name), i.IsReadOnly, i.FrameIndex, i.Id))
                        .ToList();

                    return new ListOperand<DebugIdentifierOperand>(operands);
                })
            }));

            Bind(startLabel);

            Scope = scope;
        }

        public void PopScope()
        {
            Scope.PopAction?.Invoke();

            Scope = Scope.Previous;
            Compiler.ScopeDepth--;
        }

        public virtual void PushLoop(LabelOperand continueTarget, LabelOperand breakTarget)
        {
            _loopLabels.Push(Tuple.Create(continueTarget, breakTarget));
        }

        public virtual void PopLoop()
        {
            _loopLabels.Pop();
        }

        public ConstantOperand<double> Number(double value)
        {
            return Compiler.NumberPool.GetOperand(value);
        }

        public ConstantOperand<string> String(string value)
        {
            return Compiler.StringPool.GetOperand(value);
        }

        public IdentifierOperand Identifier(string name)
        {
            return Scope.Get(name);
        }

        public bool TryGetIdentifier(string name, out IdentifierOperand identifier)
        {
            identifier = Identifier(name);
            return identifier != null;
        }

        public virtual LabelOperand ContinueLabel()
        {
            for (var i = _loopLabels.Count - 1; i >= 0; i--)
            {
                var label = _loopLabels[i].Item1;
                if (label != null)
                    return label;
            }

            return null;
        }

        public virtual LabelOperand BreakLabel()
        {
            for (var i = _loopLabels.Count - 1; i >= 0; i--)
            {
                var label = _loopLabels[i].Item2;
                if (label != null)
                    return label;
            }

            return null;
        }

        public IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            return Scope.DefineInternal(name, canHaveMultiple);
        }

        public virtual void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public int ArgIndex { get; }
        public int LocalIndex { get; }
        public Scope Scope { get; protected set; }

        public ExpressionCompiler Compiler { get; }

        public string ParentName { get; }
        public string Name { get; }
        public string FullName { get; }

        public IdentifierOperand AssignedName { get; }
        public LabelOperand Label { get; }

        public int IdentifierCount { get; protected set; }

        public FunctionContext(ExpressionCompiler compiler, int argIndex, int localIndex, Scope prevScope, string parentName, string name)
        {
            _instructions = new List<Instruction>();
            _loopLabels = new IndexedStack<Tuple<LabelOperand, LabelOperand>>();

            Compiler = compiler;
            ArgIndex = argIndex;
            LocalIndex = localIndex;

            Scope = prevScope;

            ParentName = parentName;
            Name = name;
            FullName = string.Format("{0}{1}{2}", parentName, string.IsNullOrEmpty(parentName) ? "" : ".", Name ?? "");

            AssignedName = name != null ? prevScope.Get(name) : null;
            Label = Compiler.MakeLabel("function");

            IdentifierCount = 0;
        }

        public virtual FunctionContext Root => this;

        public IEnumerable<Instruction> Instructions => _instructions;

        public virtual FunctionContext MakeFunction(string name)
        {
            name = name ?? string.Format("lambda_{0}", Compiler.LambdaId++);

            var context = new FunctionContext(Compiler, ArgIndex + 1, LocalIndex + 1, Scope, FullName, name);
            Compiler.RegisterFunction(context);
            return context;
        }

        public virtual LabelOperand MakeLabel(string name = null)
        {
            return Compiler.MakeLabel(name);
        }

        public virtual void PushScope()
        {
            Compiler.ScopeDepth++;
            var scopeId = Compiler.ScopeId++;

            // don't do extra work if we dont need it
            if (Compiler.Options.DebugInfo <= MondDebugInfoLevel.StackTrace)
            {
                Scope = new Scope(scopeId, ArgIndex, LocalIndex, Scope);
                return;
            }

            var startLabel = MakeLabel("scopeStart");
            var endLabel = MakeLabel("scopeEnd");

            var newScope = new Scope(scopeId, ArgIndex, LocalIndex, Scope, () => Bind(endLabel));

            Emit(new Instruction(InstructionType.Scope, new IInstructionOperand[]
            {
                new ImmediateOperand(scopeId),
                new ImmediateOperand(Compiler.ScopeDepth),
                new ImmediateOperand(Scope?.Id ?? -1),
                startLabel,
                endLabel,
                new DeferredOperand<ListOperand<DebugIdentifierOperand>>(() =>
                {
                    var operands = newScope.Identifiers
                        .Select(i => new DebugIdentifierOperand(String(i.Name), i.IsReadOnly, i.FrameIndex, i.Id))
                        .ToList();

                    return new ListOperand<DebugIdentifierOperand>(operands);
                })
            }));

            Bind(startLabel);

            Scope = newScope;
        }

        public virtual void PopScope()
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

        public virtual ConstantOperand<double> Number(double value)
        {
            return Compiler.NumberPool.GetOperand(value);
        }

        public virtual ConstantOperand<string> String(string value)
        {
            return Compiler.StringPool.GetOperand(value);
        }

        public virtual IdentifierOperand Identifier(string name)
        {
            return Scope.Get(name);
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

        public virtual bool DefineIdentifier(string name, bool isReadOnly = false)
        {
            var success = Scope.Define(name, isReadOnly);

            if (success)
                IdentifierCount++;

            return success;
        }

        public virtual bool DefineArgument(int index, string name)
        {
            return Scope.DefineArgument(index, name);
        }

        public virtual IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            IdentifierCount++;
            return Scope.DefineInternal(name, canHaveMultiple);
        }

        public virtual void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
        }
    }
}

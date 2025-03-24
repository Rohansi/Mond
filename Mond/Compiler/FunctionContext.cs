using System;
using System.Collections.Generic;
using System.Linq;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler
{
    internal partial class FunctionContext
    {
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public ExpressionCompiler Compiler { get; }

        public int FrameDepth => Scope.FrameDepth;
        public int LexicalDepth => Scope.LexicalDepth;
        public Scope Scope { get; protected set; }

        public string ParentName { get; }
        public string Name { get; }
        public string FullName { get; }

        public IdentifierOperand AssignedName { get; }
        public LabelOperand Label { get; }

        public bool MakeDeclarationsGlobal => FrameDepth == 0 && LexicalDepth == 0 && Compiler.Options.MakeRootDeclarationsGlobal;

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

        protected virtual FunctionContext NewContext(ExpressionCompiler compiler, Scope scope, string parentName, string name)
        {
            return new FunctionContext(compiler, scope, parentName, name);
        }

        public FunctionContext MakeFunction(string name, Scope scope, FunctionExpression functionExpression = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (scope.Previous != Scope)
            {
                throw new ArgumentException("Function scope must be linked to the current scope", nameof(scope));
            }

            if (scope.FrameDepth != (Scope?.FrameDepth ?? -1) + 1)
            {
                throw new ArgumentException("Function scope must have depth right above the current scope", nameof(scope));
            }

            name ??= $"lambda_{Compiler.LambdaId++}";

            var context = NewContext(Compiler, scope.Previous, FullName, name);
            Compiler.RegisterFunction(context);

            context.Bind(context.Label);

            if (Compiler.Options.DebugInfo >= MondDebugInfoLevel.StackTrace)
            {
                context.Emit(new Instruction(InstructionType.Function, String(context.FullName)));
            }

            int? varArgsFixedCount = functionExpression?.OtherArguments != null
                ? functionExpression.Arguments.Count
                : null;
            context.PushScope(scope, varArgsFixedCount);
            return context;
        }

        public LabelOperand MakeLabel(string name = null)
        {
            return Compiler.MakeLabel(name);
        }

        public void PushScope(Scope scope, int? varArgsFixedCount = null)
        {
            if (scope.Previous != Scope)
            {
                throw new ArgumentException("Pushed scope must be linked to the current scope", nameof(scope));
            }

            var (captureArray, captureCount) = scope.Preprocess();

            Compiler.ScopeDepth++;

            if (Compiler.Options.DebugInfo >= MondDebugInfoLevel.Full)
            {
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
                            .Select(i => new DebugIdentifierOperand(String(i.Name), i.IsReadOnly, i.IsCaptured, i is ArgumentIdentifierOperand, i.FrameIndex, i.Id))
                            .ToList();

                        return new ListOperand<DebugIdentifierOperand>(operands);
                    })
                }));

                Bind(startLabel);
            }

            Scope = scope;

            if (Scope.FrameDepth != Scope.Previous?.FrameDepth)
            {
                var currentScope = Scope; // note: capture Scope at this point in time
                var identifierCount = new DeferredOperand<ImmediateOperand>(() =>
                    new ImmediateOperand(currentScope.IdentifierCount));

                Emit(new Instruction(InstructionType.Enter, identifierCount));
            }

            if (varArgsFixedCount != null)
            {
                Emit(new Instruction(InstructionType.VarArgs, new ImmediateOperand(varArgsFixedCount.Value)));
            }

            if (captureArray != null && captureCount > 0)
            {
                NewArray(captureCount);
                Store(captureArray);
            }

            foreach (var identifier in Scope.Identifiers)
            {
                if (identifier is ArgumentIdentifierOperand { IsCaptured: true } argIdentifier)
                {
                    Emit(new Instruction(InstructionType.LdArgF, new ImmediateOperand(argIdentifier.ArgumentId)));
                    Store(identifier); // should store into the capture array
                }
            }
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

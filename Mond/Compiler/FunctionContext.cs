using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public readonly int ArgIndex;
        public readonly int LocalIndex;
        public Scope Scope { get; protected set; }

        public readonly ExpressionCompiler Compiler;

        public readonly string ParentName;
        public readonly string Name;
        public readonly string FullName;

        public readonly IdentifierOperand AssignedName;
        public readonly LabelOperand Label;

        public int IdentifierCount { get; protected set; }

        public FunctionContext(ExpressionCompiler compiler, int argIndex, int localIndex, Scope prevScope, string parentName, string name)
        {
            _instructions = new List<Instruction>();
            _loopLabels = new IndexedStack<Tuple<LabelOperand, LabelOperand>>();

            Compiler = compiler;
            ArgIndex = argIndex;
            LocalIndex = localIndex;

            Scope = new Scope(ArgIndex, LocalIndex, prevScope);

            ParentName = parentName;
            Name = name;
            FullName = string.Format("{0}{1}{2}", parentName, string.IsNullOrEmpty(parentName) ? "" : ".", Name ?? "");

            AssignedName = name != null ? prevScope.Get(name) : null;
            Label = Compiler.MakeLabel("function");

            IdentifierCount = 0;
        }

        public IEnumerable<Instruction> Instructions
        {
            get { return _instructions; }
        }

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
            Scope = new Scope(ArgIndex, LocalIndex, Scope);
        }

        public virtual void PopScope()
        {
            Scope = Scope.Previous;
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

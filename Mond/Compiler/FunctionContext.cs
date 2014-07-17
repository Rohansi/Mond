using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public readonly ExpressionCompiler Compiler;

        public readonly IdentifierOperand Name;
        public readonly LabelOperand Label;

        public int IdentifierCount { get; protected set; }

        public FunctionContext(ExpressionCompiler compiler, string name)
        {
            Compiler = compiler;
            _instructions = new List<Instruction>();
            _loopLabels = new IndexedStack<Tuple<LabelOperand, LabelOperand>>();

            Name = name != null ? Compiler.Identifier(name) : null;
            Label = Compiler.MakeLabel("function");

            IdentifierCount = 0;
        }

        public IEnumerable<Instruction> Instructions
        {
            get
            {
                foreach (var instruction in _instructions)
                {
                    if (instruction.Type == InstructionType.Enter)
                        yield return new Instruction(InstructionType.Enter, new ImmediateOperand(IdentifierCount));
                    else
                        yield return instruction;
                }
            }
        }

        public virtual FunctionContext MakeFunction(string name)
        {
            var context = new FunctionContext(Compiler, name);
            Compiler.RegisterFunction(context);
            return context;
        }

        public virtual LabelOperand MakeLabel(string name = null)
        {
            return Compiler.MakeLabel(name);
        }

        public virtual void PushScope()
        {
            Compiler.PushScope();
        }

        public virtual void PopScope()
        {
            Compiler.PopScope();
        }

        public virtual void PushFrame()
        {
            Compiler.PushFrame();
        }

        public virtual void PopFrame()
        {
            Compiler.PopFrame();
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

        public ConstantOperand<string> String(string value)
        {
            return Compiler.StringPool.GetOperand(value);
        }

        public virtual IdentifierOperand Identifier(string name)
        {
            return Compiler.Identifier(name);
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

        public virtual bool DefineIdentifier(string name, bool isReadOnly = false, bool allowOverlap = false)
        {
            var success = Compiler.DefineIdentifier(name, isReadOnly, allowOverlap);

            if (success)
                IdentifierCount++;

            return success;
        }

        public virtual bool DefineArgument(int index, string name)
        {
            return Compiler.DefineArgument(index, name);
        }

        public virtual void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
        }
    }
}

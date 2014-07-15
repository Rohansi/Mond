using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        private readonly ExpressionCompiler _compiler;
        private readonly List<Instruction> _instructions;
        private readonly IndexedStack<Tuple<LabelOperand, LabelOperand>> _loopLabels;

        public readonly IdentifierOperand Name;
        public readonly LabelOperand Label;

        public int IdentifierCount { get; private set; }

        public FunctionContext(ExpressionCompiler compiler, string name)
        {
            _compiler = compiler;
            _instructions = new List<Instruction>();
            _loopLabels = new IndexedStack<Tuple<LabelOperand, LabelOperand>>();

            Name = name != null ? _compiler.Identifier(name) : null;
            Label = _compiler.MakeLabel("function");

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

        public int FrameIndex
        {
            get { return _compiler.FrameIndex; }
        }

        public FunctionContext MakeFunction(string name)
        {
            return _compiler.MakeFunction(name);
        }

        public LabelOperand MakeLabel(string name = null)
        {
            return _compiler.MakeLabel(name);
        }

        public void PushScope()
        {
            _compiler.PushScope();
        }

        public void PopScope()
        {
            _compiler.PopScope();
        }

        public void PushFrame()
        {
            _compiler.PushFrame();
        }

        public void PopFrame()
        {
            _compiler.PopFrame();
        }

        public void PushLoop(LabelOperand continueTarget, LabelOperand breakTarget)
        {
            _loopLabels.Push(Tuple.Create(continueTarget, breakTarget));
        }

        public void PopLoop()
        {
            _loopLabels.Pop();
        }

        public ConstantOperand<double> Number(double value)
        {
            return _compiler.NumberPool.GetOperand(value);
        }

        public ConstantOperand<string> String(string value)
        {
            return _compiler.StringPool.GetOperand(value);
        }

        public IdentifierOperand Identifier(string name)
        {
            return _compiler.Identifier(name);
        }

        public LabelOperand ContinueLabel()
        {
            for (var i = _loopLabels.Count - 1; i >= 0; i--)
            {
                var label = _loopLabels[i].Item1;
                if (label != null)
                    return label;
            }

            return null;
        }

        public LabelOperand BreakLabel()
        {
            for (var i = _loopLabels.Count - 1; i >= 0; i--)
            {
                var label = _loopLabels[i].Item2;
                if (label != null)
                    return label;
            }

            return null;
        }

        public bool DefineIdentifier(string name, bool isReadOnly = false)
        {
            IdentifierCount++;
            return _compiler.DefineIdentifier(name, isReadOnly);
        }

        public bool DefineArgument(int index, string name)
        {
            return _compiler.DefineArgument(index, name);
        }

        public void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
        }
    }
}

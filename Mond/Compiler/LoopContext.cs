namespace Mond.Compiler
{
    sealed class LoopContext : FunctionContext
    {
        private readonly FunctionContext _parent;

        public LoopContext(FunctionContext parent)
            : base(parent.Compiler, parent.ArgIndex, parent.LocalIndex + 1, parent.Scope, null, parent.Name)
        {
            _parent = parent;
        }

        public override FunctionContext Root
        {
            get { return _parent.Root; }
        }

        public override void PushLoop(LabelOperand continueTarget, LabelOperand breakTarget)
        {
            _parent.PushLoop(continueTarget, breakTarget);
        }

        public override void PopLoop()
        {
            _parent.PopLoop();
        }

        public override LabelOperand ContinueLabel()
        {
            return _parent.ContinueLabel();
        }

        public override LabelOperand BreakLabel()
        {
            return _parent.BreakLabel();
        }

        public override void Emit(Instruction instruction)
        {
            _parent.Emit(instruction);
        }
    }
}

namespace Mond.Compiler.Expressions
{
    abstract class Expression
    {
        public readonly string FileName;
        public readonly int Line;

        public Expression Parent { get; private set; }

        protected Expression(string fileName, int line)
        {
            FileName = fileName;
            Line = line;
        }

        public abstract void Print(int indent);
        public abstract int Compile(FunctionContext context);
        public abstract Expression Simplify();

        public virtual void SetParent(Expression parent)
        {
            Parent = parent;
        }

        public void CheckStack(int stack, int requiredStack)
        {
            if (stack != requiredStack)
                throw new MondCompilerException(FileName, Line, CompilerError.BadStackState);
        }
    }
}

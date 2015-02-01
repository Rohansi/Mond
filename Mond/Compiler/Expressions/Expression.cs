namespace Mond.Compiler.Expressions
{
    abstract class Expression
    {
        public string FileName { get; protected set; }
        public int Line { get; protected set; }
        public int Column { get; protected set; }

        public Expression Parent { get; private set; }

        protected Expression(string fileName, int line, int column)
        {
            FileName = fileName;
            Line = line;
            Column = column;
        }

        public abstract int Compile(FunctionContext context);
        public abstract Expression Simplify();
        public abstract T Accept<T>(IExpressionVisitor<T> visitor);

        public virtual void SetParent(Expression parent)
        {
            Parent = parent;
        }

        public void CheckStack(int stack, int requiredStack)
        {
            if (stack != requiredStack)
                throw new MondCompilerException(this, CompilerError.BadStackState);
        }
    }
}

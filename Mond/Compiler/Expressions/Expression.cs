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
        public abstract int Compile(CompilerContext context);
        public abstract Expression Simplify();

        public virtual void SetParent(Expression parent)
        {
            Parent = parent;
        }

        public static void CompileCheck(CompilerContext context, Expression expression, int requiredStack)
        {
            var stack = expression.Compile(context);

            if (stack != requiredStack)
                throw new MondCompilerException(expression.FileName, expression.Line, "Bad stack state");
        }
    }
}

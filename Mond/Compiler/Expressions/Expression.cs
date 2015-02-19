namespace Mond.Compiler.Expressions
{
    abstract class Expression
    {
        public Token Token { get; protected set; }
        
        public virtual Token StartToken { get { return Token; } }
        public Token EndToken { get; set; }

        public Expression Parent { get; private set; }

        protected Expression(Token token)
        {
            Token = token;
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

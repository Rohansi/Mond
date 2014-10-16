namespace Mond.Compiler.Expressions
{
    class UnpackExpression : Expression
    {
        public Expression Right { get; private set; }

        public UnpackExpression(Token token, Expression right)
            : base(token.FileName, token.Line)
        {
            Right = right;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Unpack");

            writer.Indent++;
            Right.Print(writer);
            writer.Indent--;
        }

        public override int Compile(FunctionContext context)
        {
            var parentCall = Parent as CallExpression;
            if (parentCall == null || parentCall.Method == this)
                throw new MondCompilerException(FileName, Line, CompilerError.UnpackMustBeInCall);

            return Right.Compile(context);
        }

        public override Expression Simplify()
        {
            Right = Right.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Right.SetParent(this);
        }
    }
}

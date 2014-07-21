namespace Mond.Compiler.Expressions
{
    class TernaryExpression : Expression
    {
        public Expression Condition { get; private set; }
        public Expression IfTrue { get; private set; }
        public Expression IfFalse { get; private set; }

        public TernaryExpression(Token token, Expression condition, Expression ifTrue, Expression ifFalse)
            : base(token.FileName, token.Line)
        {
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Conditional");

            writer.WriteIndent();
            writer.WriteLine("-Expression");

            writer.Indent += 2;
            Condition.Print(writer);
            writer.Indent -= 2;

            writer.WriteIndent();
            writer.WriteLine("-True");

            writer.Indent += 2;
            IfTrue.Print(writer);
            writer.Indent -= 2;

            writer.WriteIndent();
            writer.WriteLine("-False");

            writer.Indent += 2;
            IfFalse.Print(writer);
            writer.Indent -= 2;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var falseLabel = context.MakeLabel("ternaryFalse");
            var endLabel = context.MakeLabel("ternaryEnd");

            stack += Condition.Compile(context);
            stack += context.JumpFalse(falseLabel);
            CheckStack(IfTrue.Compile(context), 1);
            stack += context.Jump(endLabel);
            stack += context.Bind(falseLabel);
            CheckStack(IfFalse.Compile(context), 1);
            stack += context.Bind(endLabel);

            CheckStack(stack, 0);
            return 1;
        }

        public override Expression Simplify()
        {
            Condition = Condition.Simplify();
            IfTrue = IfTrue.Simplify();
            IfFalse = IfFalse.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Condition.SetParent(this);
            IfTrue.SetParent(this);
            IfFalse.SetParent(this);
        }
    }
}

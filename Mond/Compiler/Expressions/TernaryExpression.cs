using System;

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

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Conditional");

            Console.Write(indentStr);
            Console.WriteLine("-Expression");

            Condition.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-True");

            IfTrue.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-False");

            IfFalse.Print(indent + 2);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var falseLabel = context.MakeLabel("ternaryFalse");
            var endLabel = context.MakeLabel("ternaryEnd");

            stack += Condition.Compile(context);
            stack += context.JumpFalse(falseLabel);
            stack += IfTrue.Compile(context);
            stack += context.Jump(endLabel);
            stack += context.Bind(falseLabel);
            stack += IfFalse.Compile(context);
            stack += context.Bind(endLabel);

            CheckStack(stack, 1);
            return stack;
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

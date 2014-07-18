using System;

namespace Mond.Compiler.Expressions.Statements
{
    class ForExpression : Expression, IStatementExpression
    {
        public Expression Initializer { get; private set; }
        public Expression Condition { get; private set; }
        public BlockExpression Increment { get; private set; }
        public BlockExpression Block { get; private set; }

        public ForExpression(Token token, Expression initializer, Expression condition, BlockExpression increment, BlockExpression block)
            : base(token.FileName, token.Line)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Block = block;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("For");

            if (Initializer != null)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Initializer");
                Initializer.Print(indent + 2);
            }

            if (Condition != null)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Condition");
                Condition.Print(indent + 2);
            }

            if (Increment != null)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Increment");
                Increment.Print(indent + 2);
            }

            Console.Write(indentStr);
            Console.WriteLine("-Block");
            Block.Print(indent + 2);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var start = context.MakeLabel("forStart");
            var increment = context.MakeLabel("forContinue");
            var end = context.MakeLabel("forEnd");

            if (Initializer != null)
                stack += Initializer.Compile(context);

            context.Bind(start);
            if (Condition != null)
            {
                stack += Condition.Compile(context);
                stack += context.JumpFalse(end);
            }

            context.PushLoop(increment, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Bind(increment);

            if (Increment != null)
                stack += Increment.Compile(context);

            stack += context.Jump(start);

            stack += context.Bind(end);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            if (Initializer != null)
                Initializer = Initializer.Simplify();

            if (Condition != null)
                Condition = Condition.Simplify();

            if (Increment != null)
                Increment = (BlockExpression)Increment.Simplify();

            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Initializer != null)
                Initializer.SetParent(this);

            if (Condition != null)
                Condition.SetParent(this);

            if (Increment != null)
                Increment.SetParent(this);

            Block.SetParent(this);
        }
    }
}

using System;

namespace Mond.Compiler.Expressions.Statements
{
    class ForExpression : Expression, IBlockStatementExpression
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

            var start = context.MakeLabel("forStart");
            var increment = context.MakeLabel("forContinue");
            var end = context.MakeLabel("forEnd");

            if (Initializer != null)
                CompileCheck(context, Initializer, 0);

            context.Bind(start);
            if (Condition != null)
                CompileCheck(context, Condition, 1);
            context.JumpFalse(end);

            context.PushLoop(increment, end);
            CompileCheck(context, Block, 0);
            context.PopLoop();

            context.Bind(increment);
            if (Increment != null)
                CompileCheck(context, Increment, 0);
            context.Jump(start);

            context.Bind(end);

            return 0;
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

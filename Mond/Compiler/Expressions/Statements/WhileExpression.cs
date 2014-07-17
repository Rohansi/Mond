using System;

namespace Mond.Compiler.Expressions.Statements
{
    class WhileExpression : Expression, IBlockStatementExpression
    {
        public Expression Condition { get; private set; }
        public BlockExpression Block { get; private set; }

        public WhileExpression(Token token, Expression condition, BlockExpression block)
            : base(token.FileName, token.Line)
        {
            Condition = condition;
            Block = block;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("While");

            Console.Write(indentStr);
            Console.WriteLine("-Condition");
            Condition.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-Do");
            Block.Print(indent + 2);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;
            var start = context.MakeLabel("whileStart");
            var end = context.MakeLabel("whileEnd");

            stack += context.Bind(start);
            stack += Condition.Compile(context);
            stack += context.JumpFalse(end);

            context.PushLoop(start, end);
            stack += Block.Compile(context);
            context.PopLoop();

            stack += context.Jump(start);
            stack += context.Bind(end);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Condition = Condition.Simplify();
            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Condition.SetParent(this);
            Block.SetParent(this);
        }
    }
}

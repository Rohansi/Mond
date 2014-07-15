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

            var start = context.MakeLabel("whileStart");
            var end = context.MakeLabel("whileEnd");

            context.Bind(start);
            CompileCheck(context, Condition, 1);
            context.JumpFalse(end);
            context.PushLoop(start, end);
            CompileCheck(context, Block, 0);
            context.PopLoop();
            context.Jump(start);
            context.Bind(end);

            return 0;
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

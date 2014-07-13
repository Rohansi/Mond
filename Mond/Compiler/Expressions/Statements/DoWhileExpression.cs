using System;

namespace Mond.Compiler.Expressions.Statements
{
    class DoWhileExpression : Expression, IBlockStatementExpression
    {
        public BlockExpression Block { get; private set; }
        public Expression Condition { get; private set; }

        public DoWhileExpression(Token token, BlockExpression block, Expression condition)
            : base(token.FileName, token.Line)
        {
            Block = block;
            Condition = condition;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("DoWhile");

            Console.Write(indentStr);
            Console.WriteLine("-Block");
            Block.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-Condition");
            Condition.Print(indent + 2);
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            var start = context.Label("doWhileStart");
            var end = context.Label("doWhileEnd");

            context.Bind(start);
            context.PushLoop(start, end);
            CompileCheck(context, Block, 0);
            context.PopLoop();
            CompileCheck(context, Condition, 1);
            context.JumpTrue(start);
            context.Bind(end);

            return 0;
        }

        public override Expression Simplify()
        {
            Block = (BlockExpression)Block.Simplify();
            Condition = Condition.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Block.SetParent(this);
            Condition.SetParent(this);
        }
    }
}

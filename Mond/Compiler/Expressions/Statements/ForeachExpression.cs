using System;

namespace Mond.Compiler.Expressions.Statements
{
    class ForeachExpression : Expression, IStatementExpression
    {
        public string Identifier { get; private set; }
        public Expression Expression { get; private set; }
        public BlockExpression Block { get; private set; }

        public ForeachExpression(Token token, string identifier, Expression expression, BlockExpression block)
            : base(token.FileName, token.Line)
        {
            Identifier = identifier;
            Expression = expression;
            Block = block;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Foreach - {0}", Identifier);

            Console.Write(indentStr);
            Console.WriteLine("-Expression");
            Expression.Print(indent + 2);

            Console.Write(indentStr);
            Console.WriteLine("-Block");
            Block.Print(indent + 2);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            context.DefineIdentifier("#enumerator", false, true);
            var enumerator = context.Identifier("#enumerator");

            var stack = 0;
            var start = context.MakeLabel("foreachStart");
            var end = context.MakeLabel("foreachEnd");

            // set enumerator
            stack += Expression.Compile(context);
            stack += context.LoadField(context.String("getEnumerator"));
            stack += context.Call(0);
            stack += context.Store(enumerator);

            // loop while moveNext returns true
            stack += context.Bind(start);
            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("moveNext"));
            stack += context.Call(0);
            stack += context.JumpFalse(end);

            // loop body
            context.PushScope();
            context.PushLoop(start, end);

            context.DefineIdentifier(Identifier, false, true);
            var identifier = context.Identifier(Identifier);

            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("current"));
            stack += context.Store(identifier);

            stack += Block.Compile(context);
            stack += context.Jump(start);

            context.PopLoop();
            context.PopScope();

            // after loop
            stack += context.Bind(end);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Expression = Expression.Simplify();
            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Expression.SetParent(this);
            Block.SetParent(this);
        }
    }
}

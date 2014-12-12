using System.Collections.Generic;

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

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var enumerator = context.DefineInternal("enumerator", true);

            var stack = 0;
            var start = context.MakeLabel("foreachStart");
            var end = context.MakeLabel("foreachEnd");

            // set enumerator
            stack += Expression.Compile(context);
            stack += context.LoadField(context.String("getEnumerator"));
            stack += context.Call(0, new List<ImmediateOperand>());
            stack += context.Store(enumerator);

            // loop while moveNext returns true
            stack += context.Bind(start);
            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("moveNext"));
            stack += context.Call(0, new List<ImmediateOperand>());
            stack += context.JumpFalse(end);

            // loop body
            context.PushScope();
            context.PushLoop(start, end);

            if (!context.DefineIdentifier(Identifier))
                throw new MondCompilerException(FileName, Line, CompilerError.IdentifierAlreadyDefined, Identifier);

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
            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("dispose"));
            stack += context.Call(0, new List<ImmediateOperand>());
            stack += context.Drop();

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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

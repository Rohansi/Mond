using System;

namespace Mond.Compiler.Expressions.Statements
{
    class ReturnExpression : Expression, IStatementExpression
    {
        public Expression Value { get; private set; }

        public ReturnExpression(Token token, Expression value)
            : base(token.FileName, token.Line)
        {
            Value = value;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Return");

            if (Value != null)
                Value.Print(indent + 1);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);
            
            if (context is SequenceBodyContext)
                throw new MondCompilerException(FileName, Line, CompilerError.ReturnInSeq);

            var stack = 0;

            if (context.Name != null)
            {
                var callExpression = Value as CallExpression;
                if (callExpression != null)
                {
                    var identifierExpression = callExpression.Method as IdentifierExpression;
                    if (identifierExpression != null && context.Identifier(identifierExpression.Name) == context.Name)
                    {
                        stack += callExpression.CompileTailCall(context);
                        CheckStack(stack, 0);
                        return stack;
                    }
                }
            }

            if (Value != null)
                stack += Value.Compile(context);
            else
                stack += context.LoadUndefined();

            stack += context.Return();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Value = Value.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Value != null)
                Value.SetParent(this);
        }
    }
}

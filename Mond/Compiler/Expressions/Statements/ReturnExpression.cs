﻿namespace Mond.Compiler.Expressions.Statements
{
    class ReturnExpression : Expression, IStatementExpression
    {
        public Expression Value { get; private set; }

        public ReturnExpression(Token token, Expression value)
            : base(token.FileName, token.Line, token.Column)
        {
            Value = value;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(FileName, Line, Column);
            
            if (context is SequenceBodyContext)
                throw new MondCompilerException(FileName, Line, Column, CompilerError.ReturnInSeq);

            var stack = 0;

            if (context.AssignedName != null)
            {
                var callExpression = Value as CallExpression;
                if (callExpression != null)
                {
                    var identifierExpression = callExpression.Method as IdentifierExpression;
                    if (identifierExpression != null && context.Identifier(identifierExpression.Name) == context.AssignedName)
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
            if (Value != null)
                Value = Value.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            if (Value != null)
                Value.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

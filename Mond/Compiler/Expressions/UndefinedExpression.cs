﻿namespace Mond.Compiler.Expressions
{
    class UndefinedExpression : Expression, IConstantExpression
    {
        public UndefinedExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Line, Column);

            return context.LoadUndefined();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public MondValue GetValue()
        {
            return MondValue.Undefined;
        }
    }
}

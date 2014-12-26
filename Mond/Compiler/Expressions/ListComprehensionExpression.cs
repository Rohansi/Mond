using System.Collections.Generic;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Expressions
{
    class ListComprehensionExpression : Expression
    {
        public BlockExpression Body { get; private set; }

        private readonly Token _token;

        public ListComprehensionExpression(Token token, BlockExpression body)
            : base(token.FileName, token.Line, token.Column)
        {
            Body = body;
            _token = token;
        }

        public override int Compile(FunctionContext context)
        {
            var seq = new SequenceExpression(_token, null, new List<string>(), null, Body);
            var expr = new CallExpression(_token, seq, new List<Expression>());

            return expr.Compile(context);
        }

        public override Expression Simplify()
        {
            Body = (BlockExpression)Body.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Body.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

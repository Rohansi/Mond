using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions
{
    class BlockExpression : Expression, IBlockExpression, IStatementExpression
    {
        public ReadOnlyCollection<Expression> Statements { get; private set; }

        public BlockExpression(Token token, IList<Expression> statements)
            : base(token)
        {
            if (Token.FileName == null)
            {
                var fileNameToken = statements.Select(e => e.Token).FirstOrDefault(t => t.FileName != null);

                if (fileNameToken != null)
                    Token = new Token(fileNameToken.FileName, Token.Line, Token.Column, Token.Type, Token.Contents);
            }

            Statements = new ReadOnlyCollection<Expression>(statements);
        }

        public BlockExpression(IList<Expression> statements)
            : this(new Token(null, -1, -1, TokenType.Eof, null), statements)
        {

        }

        public override int Compile(FunctionContext context)
        {
            var needStatements = context.Compiler.Options.DebugInfo >= MondDebugInfoLevel.Full;

            foreach (var statement in Statements)
            {
                if (needStatements)
                {
                    context.Emit(new Instruction(InstructionType.Statement, new IInstructionOperand[]
                    {
                        new ImmediateOperand(statement.StartToken.Line),
                        new ImmediateOperand(statement.StartToken.Column),
                        new ImmediateOperand(statement.EndToken.Line),
                        new ImmediateOperand(statement.EndToken.Column)
                    }));
                }

                var stack = statement.Compile(context);

                while (stack > 0)
                {
                    context.Drop();
                    stack--;
                }
            }

            return 0;
        }

        public override Expression Simplify()
        {
            Statements = Statements
                .Select(s => s.Simplify())
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var statement in Statements)
            {
                statement.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

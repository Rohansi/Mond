using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    class IfExpression : Expression, IStatementExpression
    {
        public class Branch
        {
            public Expression Condition { get; private set; }
            public BlockExpression Block { get; private set; }

            public Branch(Expression condition, BlockExpression block)
            {
                Condition = condition;
                Block = block;
            }
        }

        public ReadOnlyCollection<Branch> Branches { get; private set; }
        public Branch Else { get; private set; }

        public bool HasChildren { get { return true; } }

        public IfExpression(Token token, List<Branch> branches, Branch elseBranch)
            : base(token)
        {
            Branches = branches.AsReadOnly();
            Else = elseBranch;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var branchLabels = new List<LabelOperand>(Branches.Count);

            for (var i = 0; i < Branches.Count; i++)
            {
                branchLabels.Add(context.MakeLabel("ifBranch_" + i));
            }

            var branchElse = context.MakeLabel("ifElse");
            var branchEnd = context.MakeLabel("ifEnd");

            context.PushScope();

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];

                context.Statement(branch.Condition);
                stack += branch.Condition.Compile(context);
                stack += context.JumpTrue(branchLabels[i]);
            }

            stack += context.Jump(branchElse);

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];

                stack += context.Bind(branchLabels[i]);
                stack += branch.Block.Compile(context);
                stack += context.Jump(branchEnd);
            }

            stack += context.Bind(branchElse);

            if (Else != null)
                stack += Else.Block.Compile(context);

            stack += context.Bind(branchEnd);

            context.PopScope();

            CheckStack(stack, 0);
            return 0;
        }

        public override Expression Simplify()
        {
            Branches = Branches
                .Select(b => new Branch(b.Condition.Simplify(), (BlockExpression)b.Block.Simplify()))
                .ToList()
                .AsReadOnly();

            if (Else != null)
                Else = new Branch(null, (BlockExpression)Else.Block.Simplify());

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var branch in Branches)
            {
                branch.Condition.SetParent(this);
                branch.Block.SetParent(this);
            }

            if (Else != null)
                Else.Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

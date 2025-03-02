using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    internal class IfExpression : Expression, IStatementExpression
    {
        public class Branch
        {
            public Expression Condition { get; }
            public ScopeExpression Block { get; }

            public Branch(Expression condition, ScopeExpression block)
            {
                Condition = condition;
                Block = block;
            }
        }

        public ReadOnlyCollection<Branch> Branches { get; private set; }
        public Branch Else { get; private set; }

        public bool HasChildren => true;

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

            CheckStack(stack, 0);
            return 0;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Branches = Branches
                .Select(b => new Branch(b.Condition.Simplify(context), (ScopeExpression)b.Block.Simplify(context)))
                .ToList()
                .AsReadOnly();

            if (Else != null)
                Else = new Branch(null, (ScopeExpression)Else.Block.Simplify(context));

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

            Else?.Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

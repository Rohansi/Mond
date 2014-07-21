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

        public IfExpression(Token token, List<Branch> branches, Branch elseBranch)
            : base(token.FileName, token.Line)
        {
            Branches = branches.AsReadOnly();
            Else = elseBranch;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("If Statement");

            var first = true;

            foreach (var branch in Branches)
            {
                writer.WriteIndent();
                writer.WriteLine(first ? "-If" : "-ElseIf");
                first = false;

                writer.Indent += 2;
                branch.Condition.Print(writer);
                writer.Indent -= 2;

                writer.WriteIndent();
                writer.WriteLine(" Do");

                writer.Indent += 2;
                branch.Block.Print(writer);
                writer.Indent -= 2;
            }

            if (Else != null)
            {
                writer.WriteIndent();
                writer.WriteLine("-Else");

                writer.WriteIndent();
                writer.WriteLine(" Do");

                writer.Indent += 2;
                Else.Block.Print(writer);
                writer.Indent -= 2;
            }
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

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
    }
}

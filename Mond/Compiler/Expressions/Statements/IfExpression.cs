using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    class IfExpression : Expression, IBlockStatementExpression
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

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("If Statement");

            var first = true;

            foreach (var branch in Branches)
            {
                Console.Write(indentStr);
                Console.WriteLine(first ? "-If" : "-ElseIf");
                first = false;

                branch.Condition.Print(indent + 2);

                Console.Write(indentStr);
                Console.WriteLine("-Do");

                branch.Block.Print(indent + 2);
            }

            if (Else != null)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Else");

                Console.Write(indentStr);
                Console.WriteLine(" Do");

                Else.Block.Print(indent + 2);
            }
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            var branchLabels = new List<LabelOperand>(Branches.Count);

            for (var i = 0; i < Branches.Count; i++)
            {
                branchLabels.Add(context.Label("ifBranch_" + i));
            }

            var branchElse = context.Label("ifElse");
            var branchEnd = context.Label("ifEnd");

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];
                CompileCheck(context, branch.Condition, 1);
                context.JumpTrue(branchLabels[i]);
            }

            context.Jump(branchElse);

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];
                context.Bind(branchLabels[i]);
                CompileCheck(context, branch.Block, 0);
                context.Jump(branchEnd);
            }

            context.Bind(branchElse);

            if (Else != null)
                CompileCheck(context, Else.Block, 0);

            context.Bind(branchEnd);

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

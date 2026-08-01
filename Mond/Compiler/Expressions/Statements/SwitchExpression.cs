using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    class SwitchExpression : Expression
    {
        public class Branch
        {
            public ReadOnlyCollection<Expression> Conditions { get; }
            public ScopeExpression Block { get; }

            public Branch(List<Expression> conditions, ScopeExpression block)
            {
                Conditions = conditions?.AsReadOnly();
                Block = block;
            }
        }

        public Expression Expression { get; private set; }
        public ReadOnlyCollection<Branch> Branches { get; private set; }

        public SwitchExpression(Token token, Expression expression, List<Branch> branches)
            : base(token)
        {
            Expression = expression;
            Branches = branches.AsReadOnly();
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var caseLabels = new List<LabelOperand>(Branches.Count);

            var caseEnd = context.MakeLabel("caseEnd");
            LabelOperand caseDefault = null;
            BlockExpression defaultBlock = null;

            for (var i = 0; i < Branches.Count; i++)
            {
                var label = context.MakeLabel("caseBranch_" + i);
                caseLabels.Add(label);

                var conditions = Branches[i].Conditions;
                if (conditions.Any(c => c == null))
                {
                    caseDefault = label;

                    if (conditions.Count == 1)
                        defaultBlock = Branches[i].Block;
                }
            }

            var emptyDefault = caseDefault == null;
            if (emptyDefault)
                caseDefault = context.MakeLabel("caseDefault");

            context.Statement(Expression);
            stack += Expression.Compile(context);
            
            var branchConditions = Branches
                .Select(b => (IReadOnlyList<Expression>)b.Conditions)
                .ToList();

            var flattenedBranches = SwitchJumpTable.FlattenBranches(branchConditions, caseLabels, caseDefault);
            SwitchJumpTable.BuildTables(flattenedBranches, caseDefault, out var tables, out var rest);

            foreach (var table in tables)
            {
                var start = table.Entries[0].Value;
                var labels = table.Entries.Select(e => e.Label).ToList();

                stack += context.Dup();
                stack += context.JumpTable(start, labels);
            }

            foreach (var entry in rest)
            {
                stack += context.Dup();
                stack += entry.Condition.Compile(context);
                stack += context.BinaryOperation(TokenType.EqualTo);
                stack += context.JumpTrue(entry.Label);
            }

            stack += context.Jump(caseDefault);

            context.PushLoop(null, caseEnd);

            for (var i = 0; i < Branches.Count; i++)
            {
                var branchStack = stack;
                var branch = Branches[i];

                if (defaultBlock != null && branch.Block == defaultBlock)
                    branchStack += context.Bind(caseDefault);

                branchStack += context.Bind(caseLabels[i]);
                branchStack += context.Drop();
                branchStack += branch.Block.Compile(context);
                branchStack += context.Jump(caseEnd);

                CheckStack(branchStack, 0);
            }

            // only bind if we have no default block
            if (emptyDefault)
                stack += context.Bind(caseDefault);

            // always drop the switch value
            stack += context.Drop();

            context.PopLoop();

            stack += context.Bind(caseEnd);

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Expression = Expression.Simplify(context);

            Branches = Branches
                .Select(b =>
                {
                    var conditions = b.Conditions
                        .Select(c => c?.Simplify(context))
                        .ToList();

                    return new Branch(conditions, (ScopeExpression)b.Block.Simplify(context));
                })
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Expression.SetParent(this);

            foreach (var branch in Branches)
            {
                foreach (var condition in branch.Conditions)
                {
                    condition?.SetParent(this);
                }

                branch.Block.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

    }
}

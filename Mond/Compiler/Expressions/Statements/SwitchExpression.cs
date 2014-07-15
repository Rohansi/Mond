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
            public ReadOnlyCollection<Expression> Conditions { get; private set; }
            public BlockExpression Block { get; private set; }

            public Branch(List<Expression> conditions, BlockExpression block)
            {
                Conditions = conditions == null ? null : conditions.AsReadOnly();
                Block = block;
            }
        }

        public Expression Expression { get; private set; }
        public ReadOnlyCollection<Branch> Branches { get; private set; }
        public BlockExpression DefaultBlock { get; private set; }

        public SwitchExpression(Token token, Expression expression, List<Branch> branches, BlockExpression defaultBlock)
            : base(token.FileName, token.Line)
        {
            Expression = expression;
            Branches = branches.AsReadOnly();
            DefaultBlock = defaultBlock;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Switch");

            Console.Write(indentStr);
            Console.WriteLine("-Expression");
            Expression.Print(indent + 2);

            foreach (var branch in Branches)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Cases");

                foreach (var condition in branch.Conditions)
                {
                    condition.Print(indent + 2);
                }

                Console.Write(indentStr);
                Console.WriteLine(" Do");

                branch.Block.Print(indent + 2);
            }

            if (DefaultBlock != null)
            {
                Console.Write(indentStr);
                Console.WriteLine("-Default");
                DefaultBlock.Print(indent + 2);
            }
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var caseLabels = new List<LabelOperand>(Branches.Count);

            for (var i = 0; i < Branches.Count; i++)
            {
                caseLabels.Add(context.MakeLabel("caseBranch_" + i));
            }

            var caseDefault = context.MakeLabel("caseDefault");
            var caseEnd = context.MakeLabel("caseEnd");

            CompileCheck(context, Expression, 1);

            List<JumpTable> tables;
            List<JumpEntry> rest;
            var flattenedBranches = FlattenBranches(Branches, caseLabels);
            BuildTables(flattenedBranches, caseDefault, out tables, out rest);

            foreach (var table in tables)
            {
                var start = table.Entries[0].Value;
                var labels = table.Entries.Select(e => e.Label).ToList();

                context.Dup();
                context.JumpTable(start, labels);
            }

            foreach (var entry in rest)
            {
                context.Dup();
                CompileCheck(context, entry.Condition, 1);
                context.BinaryOperation(TokenType.EqualTo);
                context.JumpTrue(entry.Label);
            }

            context.Jump(caseDefault);

            context.PushLoop(null, caseEnd);

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];
                context.Bind(caseLabels[i]);
                context.Drop();
                CompileCheck(context, branch.Block, 0);
                context.Jump(caseEnd);
            }

            context.Bind(caseDefault);
            context.Drop();

            if (DefaultBlock != null)
                CompileCheck(context, DefaultBlock, 0);

            context.PopLoop();

            context.Bind(caseEnd);

            return 0;
        }

        public override Expression Simplify()
        {
            Expression = Expression.Simplify();

            Branches = Branches
                .Select(b =>
                {
                    var conditions = b.Conditions
                        .Select(condition => condition.Simplify())
                        .ToList();

                    return new Branch(conditions, (BlockExpression)b.Block.Simplify());
                })
                .ToList()
                .AsReadOnly();

            if (DefaultBlock != null)
                DefaultBlock = (BlockExpression)DefaultBlock.Simplify();

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
                    condition.SetParent(this);
                }
                
                branch.Block.SetParent(this);
            }

            if (DefaultBlock != null)
            {
                DefaultBlock.SetParent(this);
            }
        }

        #region Jump Table Stuff
        private class JumpEntry
        {
            public readonly Expression Condition;
            public readonly LabelOperand Label;

            public JumpEntry(Expression condition, LabelOperand label)
            {
                Condition = condition;
                Label = label;
            }
        }

        private class JumpTableEntry<T>
        {
            public readonly Expression Condition;
            public readonly T Value;
            public readonly LabelOperand Label;

            public JumpTableEntry(Expression condition, T value, LabelOperand label)
            {
                Condition = condition;
                Value = value;
                Label = label;
            }
        }

        private class JumpTable
        {
            public readonly ReadOnlyCollection<JumpTableEntry<int>> Entries;
            public readonly int Holes;

            public JumpTable(List<JumpTableEntry<int>> entries, int holes)
            {
                Entries = entries.AsReadOnly();
                Holes = holes;
            }
        }

        static IEnumerable<JumpEntry> FlattenBranches(IList<Branch> branches, IList<LabelOperand> labels)
        {
            var branchConditions = new HashSet<MondValue>();

            for (var i = 0; i < branches.Count; i++)
            {
                foreach (var condition in branches[i].Conditions)
                {
                    var constantExpression = condition as IConstantExpression;
                    if (constantExpression == null)
                        throw new MondCompilerException(condition.FileName, condition.Line, "Expected a constant value");

                    if (!branchConditions.Add(constantExpression.GetValue()))
                        throw new MondCompilerException(condition.FileName, condition.Line, "Duplicate case value");

                    yield return new JumpEntry(condition, labels[i]);
                }
            }
        }

        static void BuildTables(IEnumerable<JumpEntry> jumps, LabelOperand defaultLabel, out List<JumpTable> tables, out List<JumpEntry> rest)
        {
            rest = new List<JumpEntry>();

            var numbers = FilterJumps(jumps, rest);

            var comparer = new GenericComparer<JumpTableEntry<int>>((b1, b2) => b1.Value - b2.Value);
            numbers.Sort(comparer);

            tables = new List<JumpTable>();

            for (var i = 0; i < numbers.Count; i++)
            {
                var table = TryBuildTable(numbers, i, defaultLabel);

                if (table != null)
                {
                    tables.Add(table);
                    i += table.Entries.Count - table.Holes - 1;
                }
                else
                {
                    rest.Add(new JumpEntry(numbers[i].Condition, numbers[i].Label));
                }
            }
        }

        static List<JumpTableEntry<int>> FilterJumps(IEnumerable<JumpEntry> jumps, ICollection<JumpEntry> rest)
        {
            var numbers = new List<JumpTableEntry<int>>();

            foreach (var jump in jumps)
            {
                var condition = jump.Condition;

                var numberExpression = condition as NumberExpression;
                if (numberExpression == null)
                {
                    rest.Add(jump);
                    continue;
                }

                var number = numberExpression.Value;
                if (double.IsNaN(number) || double.IsInfinity(number) || Math.Abs(number - (int)Math.Truncate(number)) > double.Epsilon)
                {
                    rest.Add(jump);
                    continue;
                }

                numbers.Add(new JumpTableEntry<int>(jump.Condition, (int)Math.Truncate(number), jump.Label));
            }

            return numbers;
        }

        static JumpTable TryBuildTable(IList<JumpTableEntry<int>> jumps, int offset, LabelOperand defaultLabel)
        {
            var tableEntries = new List<JumpTableEntry<int>>();
            var tableHoles = 0;

            var prev = jumps[offset].Value;
            for (var i = offset; i < jumps.Count; i++)
            {
                var holeSize = jumps[i].Value - prev;
                if (holeSize < 0) throw new Exception("not sorted");

                holeSize--;

                if (holeSize > 3)
                    break;

                for (var j = 0; j < holeSize; j++)
                {
                    tableEntries.Add(new JumpTableEntry<int>(null, 0, defaultLabel));
                }

                tableEntries.Add(jumps[i]);

                tableHoles += Math.Max(holeSize, 0);
                prev = jumps[i].Value;
            }

            if (tableEntries.Count < 3)
                return null;

            if ((double)tableHoles / tableEntries.Count >= 0.25) // TODO: allow more holes for large tables?
                return null;

            return new JumpTable(tableEntries, tableHoles);
        }
        #endregion
    }
}

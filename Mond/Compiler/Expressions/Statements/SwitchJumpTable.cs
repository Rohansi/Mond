using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    /// <summary>
    /// Shared jump table building logic used by the switch statement and switch expression.
    /// </summary>
    static class SwitchJumpTable
    {
        public class JumpEntry
        {
            public Expression Condition { get; }
            public LabelOperand Label { get; }

            public JumpEntry(Expression condition, LabelOperand label)
            {
                Condition = condition;
                Label = label;
            }
        }

        public class JumpTableEntry<T>
        {
            public Expression Condition { get; }
            public T Value { get; }
            public LabelOperand Label { get; }

            public JumpTableEntry(Expression condition, T value, LabelOperand label)
            {
                Condition = condition;
                Value = value;
                Label = label;
            }
        }

        public class JumpTable
        {
            public ReadOnlyCollection<JumpTableEntry<int>> Entries { get; }
            public int Holes { get; }

            public JumpTable(List<JumpTableEntry<int>> entries, int holes)
            {
                Entries = entries.AsReadOnly();
                Holes = holes;
            }
        }

        public static IEnumerable<JumpEntry> FlattenBranches(IList<IReadOnlyList<Expression>> branchConditionGroups, IList<LabelOperand> labels, LabelOperand defaultLabel)
        {
            var branchConditions = new HashSet<MondValue>();

            for (var i = 0; i < branchConditionGroups.Count; i++)
            {
                foreach (var condition in branchConditionGroups[i])
                {
                    if (condition == null) // default
                    {
                        yield return new JumpEntry(null, defaultLabel);
                        continue;
                    }

                    var constantExpression = condition as IConstantExpression;
                    if (constantExpression == null)
                        throw new MondCompilerException(condition, CompilerError.ExpectedConstant);

                    if (!branchConditions.Add(constantExpression.GetValue()))
                        throw new MondCompilerException(condition, CompilerError.DuplicateCase);

                    yield return new JumpEntry(condition, labels[i]);
                }
            }
        }

        public static void BuildTables(IEnumerable<JumpEntry> jumps, LabelOperand defaultLabel, out List<JumpTable> tables, out List<JumpEntry> rest)
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

        private static List<JumpTableEntry<int>> FilterJumps(IEnumerable<JumpEntry> jumps, ICollection<JumpEntry> rest)
        {
            var numbers = new List<JumpTableEntry<int>>();

            foreach (var jump in jumps)
            {
                var condition = jump.Condition;

                if (condition == null) // default
                    continue;

                var numberExpression = condition as NumberExpression;
                if (numberExpression == null)
                {
                    rest.Add(jump);
                    continue;
                }

                var number = numberExpression.Value;
                if (double.IsNaN(number) || double.IsInfinity(number) || Math.Abs(number - (int)number) > double.Epsilon)
                {
                    rest.Add(jump);
                    continue;
                }

                numbers.Add(new JumpTableEntry<int>(jump.Condition, (int)number, jump.Label));
            }

            return numbers;
        }

        private static JumpTable TryBuildTable(IList<JumpTableEntry<int>> jumps, int offset, LabelOperand defaultLabel)
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
    }
}

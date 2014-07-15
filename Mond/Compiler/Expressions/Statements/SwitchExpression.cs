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
            // TODO: make this more than a fancy if statement

            context.Line(FileName, Line);

            var caseLabels = new List<LabelOperand>(Branches.Count);

            for (var i = 0; i < Branches.Count; i++)
            {
                caseLabels.Add(context.MakeLabel("caseBranch_" + i));
            }

            var caseDefault = context.MakeLabel("caseDefault");
            var caseEnd = context.MakeLabel("caseEnd");

            CompileCheck(context, Expression, 1);

            for (var i = 0; i < Branches.Count; i++)
            {
                var branch = Branches[i];

                foreach (var condition in branch.Conditions)
                {
                    if (!(condition is IConstantExpression))
                        throw new MondCompilerException(condition.FileName, condition.Line, "Expected a constant value");

                    context.Dup();
                    CompileCheck(context, condition, 1);
                    context.BinaryOperation(TokenType.EqualTo);
                    context.JumpTrue(caseLabels[i]);
                }
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
    }
}

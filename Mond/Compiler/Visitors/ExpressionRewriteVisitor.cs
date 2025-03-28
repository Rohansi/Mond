﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Visitors
{
    abstract class ExpressionRewriteVisitor : IExpressionVisitor<Expression>
    {
        #region Statements

        public virtual Expression Visit(BreakExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(ContinueExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(DebuggerExpression expression)
        {
            return expression;
        }

        public Expression Visit(DeclareGlobalsExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(DoWhileExpression expression)
        {
            return new DoWhileExpression(
                expression.Token,
                (ScopeExpression)expression.Block.Accept(this),
                expression.Condition.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ForeachExpression expression)
        {
            var destructure = expression.DestructureExpression?.Accept(this);
            return new ForeachExpression(
                expression.Token,
                expression.InToken,
                expression.Identifier,
                expression.Expression.Accept(this),
                (ScopeExpression)expression.Block.Accept(this),
                destructure)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ForExpression expression)
        {
            return new ForExpression(
                expression.Token,
                (BlockExpression)expression.Initializer?.Accept(this),
                expression.Condition?.Accept(this),
                (BlockExpression)expression.Increment?.Accept(this),
                (ScopeExpression)expression.Block.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(FunctionExpression expression)
        {
            return new FunctionExpression(
                expression.Token,
                expression.Name,
                expression.Arguments.ToList(),
                expression.OtherArguments,
                (ScopeExpression)expression.Block.Accept(this),
                expression.DebugName)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(IfExpression expression)
        {
            IfExpression.Branch VisitBranch(IfExpression.Branch b)
            {
                if (b == null) return null;

                var condition = b.Condition?.Accept(this);
                return new IfExpression.Branch(condition, (ScopeExpression)b.Block.Accept(this));
            }

            var branches = expression.Branches.Select(VisitBranch).ToList();

            return new IfExpression(expression.Token, branches, VisitBranch(expression.Else))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ReturnExpression expression)
        {
            return new ReturnExpression(expression.Token, expression.Value.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(SequenceExpression expression)
        {
            return new SequenceExpression(
                expression.Token,
                expression.Name,
                expression.Arguments.ToList(),
                expression.OtherArguments,
                (ScopeExpression)expression.Block.Accept(this),
                expression.DebugName)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(SwitchExpression expression)
        {
            SwitchExpression.Branch VisitBranch(SwitchExpression.Branch b)
            {
                if (b == null) return null;

                var conditions = b.Conditions.Select(c => c?.Accept(this)).ToList();

                return new SwitchExpression.Branch(conditions, (ScopeExpression)b.Block.Accept(this));
            }

            var branches = expression.Branches.Select(VisitBranch).ToList();

            return new SwitchExpression(expression.Token, expression.Expression.Accept(this), branches)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(VarExpression expression)
        {
            var declarations = expression.Declarations.Select(d =>
            {
                if (d.Initializer == null)
                    return new VarExpression.Declaration(d.Name, null);

                return new VarExpression.Declaration(d.Name, d.Initializer.Accept(this));
            }).ToList();

            return new VarExpression(expression.Token, declarations, expression.IsReadOnly)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(WhileExpression expression)
        {
            return new WhileExpression(
                expression.Token,
                expression.Condition.Accept(this),
                (ScopeExpression)expression.Block.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(YieldExpression expression)
        {
            return new YieldExpression(expression.Token, expression.Value.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ExportExpression expression)
        {
            return new ExportExpression(expression.Token, expression.DeclarationExpression.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ImportExpression expression)
        {
            if (expression.BindName != null)
            {
                return new ImportExpression(expression.Token, expression.ModuleName, expression.BindName)
                {
                    EndToken = expression.EndToken
                };
            }

            return new ImportExpression(expression.Token, expression.ModuleName, expression.Fields)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ExportAllExpression expression)
        {
            return new ExportAllExpression(expression.Token, expression.ModuleName)
            {
                EndToken = expression.EndToken
            };
        }

        #endregion

        public virtual Expression Visit(ArrayExpression expression)
        {
            var values = expression.Values.Select(e => e.Accept(this)).ToList();
            return new ArrayExpression(expression.Token, values)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(BinaryOperatorExpression expression)
        {
            return new BinaryOperatorExpression(
                expression.Token,
                expression.Left.Accept(this),
                expression.Right.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(BlockExpression expression)
        {
            var statements = expression.Statements.Select(s => s.Accept(this)).ToList();
            return new BlockExpression(expression.Token, statements)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(BoolExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(CallExpression expression)
        {
            var arguments = expression.Arguments.Select(e => e.Accept(this)).ToList();
            return new CallExpression(expression.Token, expression.Method.Accept(this), arguments);
        }

        public virtual Expression Visit(EmptyExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(FieldExpression expression)
        {
            return new FieldExpression(expression.Token, expression.Left.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(GlobalExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(IdentifierExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(IndexerExpression expression)
        {
            return new IndexerExpression(
                expression.Token,
                expression.Left.Accept(this),
                expression.Index.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(NullExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(NumberExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(ObjectExpression expression)
        {
            var values = expression.Values
                .Select(kv => new KeyValuePair<string, Expression>(kv.Key, kv.Value.Accept(this)))
                .ToList();

            return new ObjectExpression(expression.Token, values)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(PipelineExpression expression)
        {
            return new PipelineExpression(
                expression.Token,
                expression.Left.Accept(this),
                expression.Right.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(PostfixOperatorExpression expression)
        {
            return new PostfixOperatorExpression(expression.Token, expression.Left.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(PrefixOperatorExpression expression)
        {
            return new PrefixOperatorExpression(expression.Token, expression.Right.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(ScopeExpression expression)
        {
            var statements = expression.Statements.Select(s => s.Accept(this)).ToList();
            return new ScopeExpression(new BlockExpression(expression.Token, statements))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(SliceExpression expression)
        {
            return new SliceExpression(
                expression.Token,
                expression.Left.Accept(this),
                expression.Start?.Accept(this),
                expression.End?.Accept(this),
                expression.Step?.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(StringExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(TernaryExpression expression)
        {
            return new TernaryExpression(
                expression.Token,
                expression.Condition.Accept(this),
                expression.IfTrue.Accept(this),
                expression.IfFalse.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(UndefinedExpression expression)
        {
            return expression;
        }

        public virtual Expression Visit(UnpackExpression expression)
        {
            return new UnpackExpression(expression.Token, expression.Right.Accept(this))
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(DestructuredObjectExpression expression)
        {
            var initializer = expression.Initializer?.Accept(this);
            return new DestructuredObjectExpression(expression.Token, expression.Fields, initializer, expression.IsReadOnly)
            {
                EndToken = expression.EndToken
            };
        }

        public virtual Expression Visit(DestructuredArrayExpression expression)
        {
            var initializer = expression.Initializer?.Accept(this);
            return new DestructuredArrayExpression(expression.Token, expression.Indices, initializer, expression.IsReadOnly)
            {
                EndToken = expression.EndToken
            };
        }
    }
}

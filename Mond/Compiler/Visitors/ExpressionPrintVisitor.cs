using System;
using System.IO;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Visitors
{
    sealed class ExpressionPrintVisitor : IExpressionVisitor<int>, IDisposable
    {
        private IndentTextWriter _writer;

        public ExpressionPrintVisitor(TextWriter writer)
        {
            _writer = new IndentTextWriter(writer);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _writer = null;
        }

        #region Statements

        public int Visit(BreakExpression expression)
        {
            _writer.WriteLine("Break");
            return 0;
        }

        public int Visit(ContinueExpression expression)
        {
            _writer.WriteLine("Continue");
            return 0;
        }

        public int Visit(DoWhileExpression expression)
        {
            _writer.WriteLine("DoWhile");

            _writer.WriteLine("-Block");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-Condition");

            _writer.Indent += 2;
            expression.Condition.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(ForeachExpression expression)
        {
            _writer.WriteLine("Foreach - {0}", expression.Identifier);

            _writer.WriteLine("-Expression");

            _writer.Indent += 2;
            expression.Expression.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-Block");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(ForExpression expression)
        {
            _writer.WriteLine("For");

            if (expression.Initializer != null)
            {
                _writer.WriteLine("-Initializer");

                _writer.Indent += 2;
                expression.Initializer.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.Condition != null)
            {
                _writer.WriteLine("-Condition");

                _writer.Indent += 2;
                expression.Condition.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.Increment != null)
            {
                _writer.WriteLine("-Increment");

                _writer.Indent += 2;
                expression.Increment.Accept(this);
                _writer.Indent -= 2;
            }

            _writer.WriteLine("-Block");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(FunctionExpression expression)
        {
            _writer.WriteLine("Function " + expression.Name);

            _writer.WriteLine("-Arguments: {0}", string.Join(", ", expression.Arguments));

            if (expression.OtherArguments != null)
                _writer.WriteLine("-Other Arguments: {0}", expression.OtherArguments);

            _writer.WriteLine("-Block");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(IfExpression expression)
        {
            _writer.WriteLine("If Statement");

            var first = true;

            foreach (var branch in expression.Branches)
            {
                _writer.WriteLine(first ? "-If" : "-ElseIf");
                first = false;

                _writer.Indent += 2;
                branch.Condition.Accept(this);
                _writer.Indent -= 2;

                _writer.WriteLine(" Do");

                _writer.Indent += 2;
                branch.Block.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.Else != null)
            {
                _writer.WriteLine("-Else");

                _writer.WriteLine(" Do");

                _writer.Indent += 2;
                expression.Else.Block.Accept(this);
                _writer.Indent -= 2;
            }

            return 0;
        }

        public int Visit(ReturnExpression expression)
        {
            _writer.WriteLine("Return");

            if (expression.Value != null)
            {
                _writer.Indent++;
                expression.Value.Accept(this);
                _writer.Indent--;
            }

            return 0;
        }

        public int Visit(SequenceExpression expression)
        {
            _writer.WriteLine("Sequence " + expression.Name);

            _writer.WriteLine("-Arguments: {0}", string.Join(", ", expression.Arguments));

            if (expression.OtherArguments != null)
                _writer.WriteLine("-Other Arguments: {0}", expression.OtherArguments);

            _writer.WriteLine("-Block");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(SwitchExpression expression)
        {
            _writer.WriteLine("Switch");

            _writer.WriteLine("-Expression");

            _writer.Indent += 2;
            expression.Expression.Accept(this);
            _writer.Indent -= 2;

            foreach (var branch in expression.Branches)
            {
                _writer.WriteLine("-Cases");

                _writer.Indent += 2;
                foreach (var condition in branch.Conditions)
                {
                    condition.Accept(this);
                }
                _writer.Indent -= 2;

                _writer.WriteLine(" Do");

                _writer.Indent += 2;
                branch.Block.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.DefaultBlock != null)
            {
                _writer.WriteLine("-Default");

                _writer.Indent += 2;
                expression.DefaultBlock.Accept(this);
                _writer.Indent -= 2;
            }

            return 0;
        }

        public int Visit(VarExpression expression)
        {
            _writer.WriteLine(expression.IsReadOnly ? "Const" : "Var");

            foreach (var declaration in expression.Declarations)
            {
                _writer.WriteLine("-" + declaration.Name + (declaration.Initializer != null ? " =" : ""));

                if (declaration.Initializer != null)
                {
                    _writer.Indent += 2;
                    declaration.Initializer.Accept(this);
                    _writer.Indent -= 2;
                }
            }

            return 0;
        }

        public int Visit(WhileExpression expression)
        {
            _writer.WriteLine("While");

            _writer.WriteLine("-Condition");

            _writer.Indent += 2;
            expression.Condition.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-Do");

            _writer.Indent += 2;
            expression.Block.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(YieldBreakExpression expression)
        {
            _writer.WriteLine("YieldBreak");
            return 0;
        }

        public int Visit(YieldExpression expression)
        {
            _writer.WriteLine("Yield");

            _writer.Indent++;
            expression.Value.Accept(this);
            _writer.Indent--;

            return 0;
        }

        #endregion

        #region Expressions

        public int Visit(ArrayExpression expression)
        {
            _writer.WriteLine("Array");

            _writer.Indent++;
            foreach (var value in expression.Values)
            {
                value.Accept(this);
            }
            _writer.Indent--;

            return 0;
        }

        public int Visit(BinaryOperatorExpression expression)
        {
            _writer.WriteLine("Operator {0}", expression.Operation);

            _writer.Indent++;
            expression.Left.Accept(this);
            expression.Right.Accept(this);
            _writer.Indent--;

            return 0;
        }

        public int Visit(BlockExpression expression)
        {
            foreach (var statement in expression.Statements)
            {
                statement.Accept(this);
            }

            return 0;
        }

        public int Visit(BoolExpression expression)
        {
            _writer.WriteLine("bool: {0}", expression.Value);
            return 0;
        }

        public int Visit(CallExpression expression)
        {
            _writer.WriteLine("Call");

            _writer.WriteLine("-Expression");

            _writer.Indent += 2;
            expression.Method.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-Arguments");

            _writer.Indent += 2;
            foreach (var arg in expression.Arguments)
            {
                arg.Accept(this);
            }
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(EmptyExpression expression)
        {
            _writer.WriteLine("Empty");
            return 0;
        }

        public int Visit(FieldExpression expression)
        {
            _writer.WriteLine("Field {0}", expression.Name);

            _writer.Indent++;
            expression.Left.Accept(this);
            _writer.Indent--;

            return 0;
        }

        public int Visit(GlobalExpression expression)
        {
            _writer.WriteLine("global");
            return 0;
        }

        public int Visit(IdentifierExpression expression)
        {
            _writer.WriteLine("identifier: {0}", expression.Name);
            return 0;
        }

        public int Visit(IndexerExpression expression)
        {
            _writer.WriteLine("Indexer");

            _writer.WriteLine("-Left");

            _writer.Indent += 2;
            expression.Left.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-Index");

            _writer.Indent += 2;
            expression.Index.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(ListComprehensionExpression expression)
        {
            _writer.WriteLine("List Comprehension");

            _writer.WriteLine("-Body");

            _writer.Indent += 2;
            expression.Body.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(NullExpression expression)
        {
            _writer.WriteLine("null");
            return 0;
        }

        public int Visit(NumberExpression expression)
        {
            _writer.WriteLine("number: {0}", expression.Value);
            return 0;
        }

        public int Visit(ObjectExpression expression)
        {
            _writer.WriteLine("Object");

            foreach (var value in expression.Values)
            {
                _writer.WriteLine("-" + value.Key);

                _writer.Indent += 2;
                value.Value.Accept(this);
                _writer.Indent -= 2;
            }

            return 0;
        }

        public int Visit(PipelineExpression expression)
        {
            _writer.WriteLine("Pipeline");

            _writer.Indent++;
            expression.Left.Accept(this);
            expression.Right.Accept(this);
            _writer.Indent--;

            return 0;
        }

        public int Visit(PostfixOperatorExpression expression)
        {
            var discardResult = expression.Parent == null || expression.Parent is BlockExpression;

            _writer.WriteLine("Postfix {0}" + (discardResult ? " - Result not used" : ""), expression.Operation);

            _writer.Indent++;
            expression.Left.Accept(this);
            _writer.Indent--;

            return 0;
        }

        public int Visit(PrefixOperatorExpression expression)
        {
            _writer.WriteLine("Prefix {0}", expression.Operation);

            _writer.Indent++;
            expression.Right.Accept(this);
            _writer.Indent--;

            return 0;
        }

        public int Visit(ScopeExpression expression)
        {
            foreach (var statement in expression.Statements)
            {
                statement.Accept(this);
            }

            return 0;
        }

        public int Visit(SliceExpression expression)
        {
            _writer.WriteLine("Slice");

            _writer.WriteLine("-Left");

            _writer.Indent += 2;
            expression.Left.Accept(this);
            _writer.Indent -= 2;

            if (expression.Start != null)
            {
                _writer.WriteLine("-Start");

                _writer.Indent += 2;
                expression.Start.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.End != null)
            {
                _writer.WriteLine("-End");

                _writer.Indent += 2;
                expression.End.Accept(this);
                _writer.Indent -= 2;
            }

            if (expression.Step != null)
            {
                _writer.WriteLine("-Step");

                _writer.Indent += 2;
                expression.Step.Accept(this);
                _writer.Indent -= 2;
            }

            return 0;
        }

        public int Visit(StringExpression expression)
        {
            _writer.WriteLine("string: \"{0}\"", expression.Value);
            return 0;
        }

        public int Visit(TernaryExpression expression)
        {
            _writer.WriteLine("Conditional");

            _writer.WriteLine("-Expression");

            _writer.Indent += 2;
            expression.Condition.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-True");

            _writer.Indent += 2;
            expression.IfTrue.Accept(this);
            _writer.Indent -= 2;

            _writer.WriteLine("-False");

            _writer.Indent += 2;
            expression.IfFalse.Accept(this);
            _writer.Indent -= 2;

            return 0;
        }

        public int Visit(UndefinedExpression expression)
        {
            _writer.WriteLine("undefined");
            return 0;
        }

        public int Visit(UnpackExpression expression)
        {
            _writer.WriteLine("Unpack");

            _writer.Indent++;
            expression.Right.Accept(this);
            _writer.Indent--;

            return 0;
        }

        #endregion
    }
}

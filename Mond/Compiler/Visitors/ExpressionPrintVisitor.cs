using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Visitors
{
    sealed class ExpressionPrintVisitor : IExpressionVisitor<int>, IDisposable
    {
        #region Static

        private static readonly Dictionary<TokenType, string> OperatorMap;

        static ExpressionPrintVisitor()
        {
            OperatorMap = new Dictionary<TokenType, string>
            {
                { TokenType.Assign, "=" },

                { TokenType.Add, "+" },
                { TokenType.Subtract, "-" },
                { TokenType.Multiply, "*" },
                { TokenType.Divide, "/" },
                { TokenType.Modulo, "%" },
                { TokenType.Exponent, "**" },
                { TokenType.BitAnd, "&" },
                { TokenType.BitOr, "|" },
                { TokenType.BitXor, "^" },
                { TokenType.BitNot, "~" },
                { TokenType.BitLeftShift, "<<" },
                { TokenType.BitRightShift, ">>" },
                { TokenType.Increment, "++" },
                { TokenType.Decrement, "--" },

                { TokenType.AddAssign, "+=" },
                { TokenType.SubtractAssign, "-=" },
                { TokenType.MultiplyAssign, "*=" },
                { TokenType.DivideAssign, "/=" },
                { TokenType.ModuloAssign, "%=" },
                { TokenType.ExponentAssign, "**=" },
                { TokenType.BitAndAssign, "&=" },
                { TokenType.BitOrAssign, "|=" },
                { TokenType.BitXorAssign, "^=" },
                { TokenType.BitLeftShiftAssign, "<<=" },
                { TokenType.BitRightShiftAssign, ">>=" },

                { TokenType.EqualTo, "==" },
                { TokenType.NotEqualTo, "!=" },
                { TokenType.GreaterThan, ">" },
                { TokenType.GreaterThanOrEqual, ">=" },
                { TokenType.LessThan, "<" },
                { TokenType.LessThanOrEqual, "<=" },
                { TokenType.Not, "!" },
                { TokenType.ConditionalAnd, "&&" },
                { TokenType.ConditionalOr, "||" },

                { TokenType.In, "in" },
                { TokenType.NotIn, "!in" },
            };
        }

        #endregion

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
            _writer.Write("break");
            return 0;
        }

        public int Visit(ContinueExpression expression)
        {
            _writer.Write("continue");
            return 0;
        }

        public int Visit(DebuggerExpression expression)
        {
            _writer.Write("debugger");
            return 0;
        }

        public int Visit(DoWhileExpression expression)
        {
            _writer.WriteLine("do");

            expression.Block.Accept(this);

            _writer.Write(" while (");
            expression.Condition.Accept(this);
            _writer.Write(")");

            return 0;
        }

        public int Visit(ForeachExpression expression)
        {
            _writer.Write("foreach (");

            if (expression.DestructureExpression != null)
                expression.DestructureExpression.Accept(this);
            else
                _writer.Write("var {0}", expression.Identifier);

            _writer.Write(" in ");
            expression.Expression.Accept(this);
            _writer.WriteLine(")");

            expression.Block.Accept(this);
            return 0;
        }

        public int Visit(ForExpression expression)
        {
            _writer.Write("for (");

            if (expression.Initializer != null)
            {
                expression.Initializer.Statements[0].Accept(this);
            }
            _writer.Write("; ");

            if (expression.Condition != null)
            {
                expression.Condition.Accept(this);
            }
            _writer.Write("; ");

            if (expression.Increment != null)
            {
                var incrementExprs = expression.Increment.Statements;
                for (var i = 0; i < incrementExprs.Count; i++)
                {
                    incrementExprs[i].Accept(this);

                    if (i < incrementExprs.Count - 1)
                        _writer.Write(", ");
                }
            }
            _writer.WriteLine(")");

            expression.Block.Accept(this);
            return 0;
        }

        public int Visit(FunctionExpression expression)
        {
            _writer.Write("fun {0}(", expression.Name);

            for (var i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    _writer.Write(", ");

                _writer.Write(expression.Arguments[i]);
            }

            if (expression.OtherArguments != null)
            {
                if (expression.Arguments.Count > 0)
                    _writer.Write(", ");

                _writer.Write("...{0}", expression.OtherArguments);
            }

            _writer.WriteLine(")");

            expression.Block.Accept(this);
            return 0;
        }

        public int Visit(IfExpression expression)
        {
            for (var i = 0; i < expression.Branches.Count; i++)
            {
                var branch = expression.Branches[i];

                _writer.Write(i == 0 ? "if" : "else if");

                _writer.Write(" (");
                branch.Condition.Accept(this);
                _writer.WriteLine(")");

                branch.Block.Accept(this);

                if (expression.Else != null || i < expression.Branches.Count - 1)
                    _writer.WriteLine();
            }

            if (expression.Else != null)
            {
                _writer.WriteLine("else ");
                expression.Else.Block.Accept(this);
            }

            return 0;
        }

        public int Visit(ReturnExpression expression)
        {
            _writer.Write("return");

            if (expression.Value != null)
            {
                _writer.Write(" ");
                expression.Value.Accept(this);
            }

            return 0;
        }

        public int Visit(SequenceExpression expression)
        {
            _writer.Write("seq {0}(", expression.Name);

            for (var i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    _writer.Write(", ");

                _writer.Write(expression.Arguments[i]);
            }

            if (expression.OtherArguments != null)
            {
                if (expression.Arguments.Count > 0)
                    _writer.Write(", ");

                _writer.Write("...{0}", expression.OtherArguments);
            }

            _writer.WriteLine(")");

            expression.Block.Accept(this);
            return 0;
        }

        public int Visit(SwitchExpression expression)
        {
            _writer.Write("switch (");
            expression.Expression.Accept(this);
            _writer.WriteLine(")");

            _writer.WriteLine("{");
            _writer.Indent++;

            for (var i = 0; i < expression.Branches.Count; i++)
            {
                var branch = expression.Branches[i];

                foreach (var condition in branch.Conditions)
                {
                    if (condition == null)
                    {
                        _writer.WriteLine("default:");
                        continue;
                    }

                    _writer.Write("case ");
                    condition.Accept(this);
                    _writer.WriteLine(":");
                }

                branch.Block.Accept(this);

                if (i < expression.Branches.Count - 1)
                    _writer.WriteLine();
            }

            _writer.WriteLine();

            _writer.Indent--;
            _writer.Write("}");

            return 0;
        }

        public int Visit(VarExpression expression)
        {
            _writer.Write(expression.IsReadOnly ? "const " : "var ");

            var first = true;

            foreach (var declaration in expression.Declarations)
            {
                if (first)
                    first = false;
                else
                    _writer.Write(", ");

                _writer.Write(declaration.Name);

                if (declaration.Initializer != null)
                {
                    _writer.Write(" = ");
                    declaration.Initializer.Accept(this);
                }
            }

            return 0;
        }

        public int Visit(WhileExpression expression)
        {
            _writer.Write("while (");
            expression.Condition.Accept(this);
            _writer.WriteLine(")");

            expression.Block.Accept(this);
            return 0;
        }

        public int Visit(YieldExpression expression)
        {
            var needParens = !(expression.Parent is IBlockExpression);

            if (needParens)
                _writer.Write('(');

            _writer.Write("yield ");
            expression.Value.Accept(this);

            if (needParens)
                _writer.Write(')');

            return 0;
        }

        public int Visit(DestructuredObjectExpression expression)
        {
            _writer.Write("{0} {{ ", expression.IsReadOnly ? "const" : "var");

            var fields = expression.Fields.Select(field => field.Alias != null ? string.Format("{0}: {1}", field.Name, field.Alias) : field.Name).ToArray();
            _writer.Write(string.Join(", ", fields));
            _writer.Write(" }");

            if (expression.Initializer != null)
            {
                _writer.Write(" = ");
                expression.Initializer.Accept(this);
            }

            return 0;
        }

        public int Visit(DestructuredArrayExpression expression)
        {
            _writer.Write("{0} [", expression.IsReadOnly ? "const" : "var");

            var indices = expression.Indices.Select(index => (index.IsSlice ? "..." : "") + index.Name).ToArray();
            _writer.Write(string.Join(", ", indices));
            _writer.Write(" ]");

            if (expression.Initializer != null)
            {
                _writer.Write(" = ");
                expression.Initializer.Accept(this);
            }

            return 0;
        }

        public int Visit(ExportExpression expression)
        {
            _writer.Write("export ");
            expression.DeclarationExpression.Accept(this);
            return 0;
        }

        #endregion

        #region Expressions

        public int Visit(ArrayExpression expression)
        {
            if (expression.Values.Count == 0)
            {
                _writer.Write("[]");
                return 0;
            }

            _writer.WriteLine("[");

            var first = true;

            _writer.Indent++;
            foreach (var value in expression.Values)
            {
                if (first)
                    first = false;
                else
                    _writer.WriteLine(", ");

                value.Accept(this);
            }
            _writer.Indent--;

            _writer.WriteLine();
            _writer.Write("]");
            return 0;
        }

        public int Visit(BinaryOperatorExpression expression)
        {
            _writer.Write("(");
            expression.Left.Accept(this);
            _writer.Write(" {0} ", OperatorMap[expression.Operation]);
            expression.Right.Accept(this);
            _writer.Write(")");

            return 0;
        }

        public int Visit(BlockExpression expression)
        {
            _writer.WriteLine("{");
            _writer.Indent++;

            foreach (var statement in expression.Statements)
            {
                statement.Accept(this);
                _writer.WriteLine(";");
            }

            _writer.Indent--;
            _writer.Write("}");

            return 0;
        }

        public int Visit(BoolExpression expression)
        {
            _writer.Write(expression.Value ? "true" : "false");
            return 0;
        }

        public int Visit(CallExpression expression)
        {
            expression.Method.Accept(this);

            _writer.Write("(");

            var first = true;
            foreach (var arg in expression.Arguments)
            {
                if (first)
                    first = false;
                else
                    _writer.Write(", ");

                arg.Accept(this);
            }

            _writer.Write(")");
            return 0;
        }

        public int Visit(EmptyExpression expression)
        {
            _writer.Write("/* empty */");
            return 0;
        }

        public int Visit(FieldExpression expression)
        {
            expression.Left.Accept(this);
            _writer.Write(".");
            _writer.Write(expression.Name);

            return 0;
        }

        public int Visit(GlobalExpression expression)
        {
            _writer.Write("global");
            return 0;
        }

        public int Visit(IdentifierExpression expression)
        {
            _writer.Write(expression.Name);
            return 0;
        }

        public int Visit(IndexerExpression expression)
        {
            expression.Left.Accept(this);
            _writer.Write("[");
            expression.Index.Accept(this);
            _writer.Write("]");

            return 0;
        }

        public int Visit(NullExpression expression)
        {
            _writer.Write("null");
            return 0;
        }

        public int Visit(NumberExpression expression)
        {
            _writer.Write(expression.Value);
            return 0;
        }

        public int Visit(ObjectExpression expression)
        {
            if (expression.Values.Count == 0)
            {
                _writer.Write("{}");
                return 0;
            }

            _writer.WriteLine("{");

            var first = true;

            _writer.Indent++;
            foreach (var value in expression.Values)
            {
                if (first)
                    first = false;
                else
                    _writer.WriteLine(", ");

                _writer.Write("{0}: ", value.Key);
                value.Value.Accept(this);
            }
            _writer.Indent--;

            _writer.WriteLine();
            _writer.Write("}");
            return 0;
        }

        public int Visit(PipelineExpression expression)
        {
            expression.Left.Accept(this);
            _writer.Write(" |> ");
            expression.Right.Accept(this);

            return 0;
        }

        public int Visit(PostfixOperatorExpression expression)
        {
            expression.Left.Accept(this);
            _writer.Write(OperatorMap[expression.Operation]);

            return 0;
        }

        public int Visit(PrefixOperatorExpression expression)
        {
            _writer.Write(OperatorMap[expression.Operation]);
            expression.Right.Accept(this);

            return 0;
        }

        public int Visit(ScopeExpression expression)
        {
            return Visit((BlockExpression)expression);
        }

        public int Visit(SliceExpression expression)
        {
            expression.Left.Accept(this);
            _writer.Write("[");

            if (expression.Start != null)
                expression.Start.Accept(this);
            _writer.Write(":");

            if (expression.End != null)
                expression.End.Accept(this);
            _writer.Write(":");

            if (expression.Step != null)
                expression.Step.Accept(this);

            _writer.Write("]");
            return 0;
        }

        public int Visit(StringExpression expression)
        {
            MondValue.String(expression.Value).Serialize(_writer);
            return 0;
        }

        public int Visit(TernaryExpression expression)
        {
            _writer.Write("(");
            expression.Condition.Accept(this);
            _writer.Write(" ? ");
            expression.IfTrue.Accept(this);
            _writer.Write(" : ");
            expression.IfFalse.Accept(this);
            _writer.Write(")");

            return 0;
        }

        public int Visit(UndefinedExpression expression)
        {
            _writer.Write("undefined");
            return 0;
        }

        public int Visit(UnpackExpression expression)
        {
            _writer.Write("...");
            expression.Right.Accept(this);

            return 0;
        }

        #endregion

    }
}

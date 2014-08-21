using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    class BinaryOperatorExpression : Expression
    {
        public TokenType Operation { get; private set; }
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public BinaryOperatorExpression(Token token, Expression left, Expression right)
            : base(token.FileName, token.Line)
        {
            Operation = token.Type;
            Left = left;
            Right = right;
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("Operator {0}", Operation);

            writer.Indent++;
            Left.Print(writer);
            Right.Print(writer);
            writer.Indent--;
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;

            TokenType assignOperation;
            var hasAssignOperation = _assignMap.TryGetValue(Operation, out assignOperation);
            var isAssign = Operation == TokenType.Assign || hasAssignOperation;

            if (isAssign)
            {
                var storable = Left as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(FileName, Line, CompilerError.LeftSideMustBeStorable);

                var needResult = !(Parent is BlockExpression);

                stack += Right.Compile(context);

                if (hasAssignOperation)
                {
                    stack += Left.Compile(context);
                    stack += context.BinaryOperation(assignOperation);
                }

                if (needResult)
                    stack += context.Dup();

                stack += storable.CompileStore(context);

                CheckStack(stack, needResult ? 1 : 0);
                return stack;
            }

            if (Operation == TokenType.ConditionalOr)
            {
                var endOr = context.MakeLabel("endOr");
                stack += Left.Compile(context);
                stack += context.JumpTruePeek(endOr);
                stack += context.Drop();
                stack += Right.Compile(context);
                stack += context.Bind(endOr);

                CheckStack(stack, 1);
                return stack;
            }

            if (Operation == TokenType.ConditionalAnd)
            {
                var endAnd = context.MakeLabel("endAnd");
                stack += Left.Compile(context);
                stack += context.JumpFalsePeek(endAnd);
                stack += context.Drop();
                stack += Right.Compile(context);
                stack += context.Bind(endAnd);

                CheckStack(stack, 1);
                return stack;
            }

            stack += Right.Compile(context);
            stack += Left.Compile(context);
            stack += context.BinaryOperation(Operation);

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify()
        {
            Left = Left.Simplify();
            Right = Right.Simplify();

            Func<double, double, double> simplifyOp;
            if (_simplifyMap.TryGetValue(Operation, out simplifyOp))
            {
                var leftNum = Left as NumberExpression;
                var rigthNum = Right as NumberExpression;

                if (leftNum != null && rigthNum != null)
                {
                    var result = simplifyOp(leftNum.Value, rigthNum.Value);
                    var token = new Token(FileName, Line, TokenType.Number, null);
                    return new NumberExpression(token, result);
                }
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Left.SetParent(this);
            Right.SetParent(this);
        }

        private static Dictionary<TokenType, TokenType> _assignMap;
        private static Dictionary<TokenType, Func<double, double, double>> _simplifyMap; 

        static BinaryOperatorExpression()
        {
            _assignMap = new Dictionary<TokenType, TokenType>
            {
                { TokenType.AddAssign, TokenType.Add },
                { TokenType.SubtractAssign, TokenType.Subtract },
                { TokenType.MultiplyAssign, TokenType.Multiply },
                { TokenType.DivideAssign, TokenType.Divide },
                { TokenType.ModuloAssign, TokenType.Modulo }
            };

            _simplifyMap = new Dictionary<TokenType, Func<double, double, double>>
            {
                { TokenType.Add, (x, y) => x + y },
                { TokenType.Subtract, (x, y) => x - y },
                { TokenType.Multiply, (x, y) => x * y },
                { TokenType.Divide, (x, y) => x / y },
                { TokenType.Modulo, (x, y) => x % y }
            };
        }
    }
}

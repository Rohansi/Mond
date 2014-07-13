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

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Operator {0}", Operation);

            Left.Print(indent + 1);
            Right.Print(indent + 1);
        }

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            TokenType assignOperation;

            var hasAssignOperation = _assignMap.TryGetValue(Operation, out assignOperation);
            var isAssign = Operation == TokenType.Assign || hasAssignOperation;

            if (isAssign)
            {
                var storable = Left as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(FileName, Line, "The left-hand side of an assignment must be storable");

                var needResult = !(Parent is BlockExpression);
                var stack = 0;

                CompileCheck(context, Right, 1);

                if (hasAssignOperation)
                {
                    CompileCheck(context, Left, 1);
                    context.BinaryOperation(assignOperation);
                }

                if (needResult)
                {
                    context.Dup();
                    stack++;
                }

                storable.CompileStore(context);
                return stack;
            }

            if (Operation == TokenType.LogicalOr)
            {
                var endOr = context.Label("endOr");
                CompileCheck(context, Left, 1);
                context.JumpTruePeek(endOr);
                CompileCheck(context, Right, 1);
                context.Bind(endOr);
                return 1;
            }

            if (Operation == TokenType.LogicalAnd)
            {
                var endAnd = context.Label("endAnd");
                CompileCheck(context, Left, 1);
                context.JumpFalsePeek(endAnd);
                CompileCheck(context, Right, 1);
                context.Bind(endAnd);
                return 1;
            }

            CompileCheck(context, Right, 1);
            CompileCheck(context, Left, 1);
            context.BinaryOperation(Operation);
            return 1;
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

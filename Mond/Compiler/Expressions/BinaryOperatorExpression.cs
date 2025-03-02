using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    class BinaryOperatorExpression : Expression
    {
        public TokenType Operation { get; }
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public override Token StartToken => Left.StartToken;

        public BinaryOperatorExpression(Token token, Expression left, Expression right)
            : base(token)
        {
            Operation = token.Type;
            Left = left;
            Right = right;
        }

        public override int Compile(FunctionContext context)
        {
            var stack = 0;
            
            var isAssignOperation = _assignMap.TryGetValue(Operation, out var assignOperation);

            if (IsAssign)
            {
                var storable = Left as IStorableExpression;
                if (storable == null)
                    throw new MondCompilerException(this, CompilerError.LeftSideMustBeStorable);

                var needResult = !(Parent is IBlockExpression);

                if (isAssignOperation)
                {
                    int preTotal;
                    var preTimes = needResult ? 3 : 2;
                    
                    stack += preTotal = storable.CompilePreLoadStore(context, preTimes);

                    stack += storable.CompileLoad(context);
                    stack += Right.Compile(context);
                    stack += context.BinaryOperation(assignOperation);

                    switch (preTotal / preTimes)
                    {
                        case 0:
                            break;
                        case 1:
                            stack += context.Swap();
                            break;
                        case 2:
                            stack += context.Swap1For2();
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    stack += storable.CompileStore(context);

                    if (needResult)
                        stack += storable.CompileLoad(context);
                }
                else
                {
                    stack += Right.Compile(context);

                    if (needResult)
                        stack += context.Dup();

                    stack += storable.CompilePreLoadStore(context, 1);
                    stack += storable.CompileStore(context);
                }

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

            stack += Left.Compile(context);
            stack += Right.Compile(context);

            context.Position(Token); // debug info
            stack += context.BinaryOperation(Operation);

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Left = Left.Simplify(context);
            Right = Right.Simplify(context);

            if (_simplifyMap.TryGetValue(Operation, out var simplifyOp))
            {
                var leftNum = Left as NumberExpression;
                var rightNum = Right as NumberExpression;

                if (leftNum != null && rightNum != null)
                {
                    var result = simplifyOp(leftNum.Value, rightNum.Value);
                    return new NumberExpression(Token, result) { EndToken = EndToken };
                }
            }

            if (Operation == TokenType.Add)
            {
                var leftStr = Left as StringExpression;
                var rightStr = Right as StringExpression;

                if (leftStr != null && rightStr != null)
                {
                    var result = leftStr.Value + rightStr.Value;
                    return new StringExpression(Token, result) { EndToken = EndToken };
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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public bool IsAssign => Operation == TokenType.Assign || _assignMap.ContainsKey(Operation);

        private static readonly Dictionary<TokenType, TokenType> _assignMap;
        private static readonly Dictionary<TokenType, Func<double, double, double>> _simplifyMap; 

        static BinaryOperatorExpression()
        {
            _assignMap = new Dictionary<TokenType, TokenType>
            {
                { TokenType.AddAssign, TokenType.Add },
                { TokenType.SubtractAssign, TokenType.Subtract },
                { TokenType.MultiplyAssign, TokenType.Multiply },
                { TokenType.DivideAssign, TokenType.Divide },
                { TokenType.ModuloAssign, TokenType.Modulo },
                { TokenType.ExponentAssign, TokenType.Exponent },
                { TokenType.BitLeftShiftAssign, TokenType.BitLeftShift },
                { TokenType.BitRightShiftAssign, TokenType.BitRightShift },
                { TokenType.BitAndAssign, TokenType.BitAnd },
                { TokenType.BitOrAssign, TokenType.BitOr },
                { TokenType.BitXorAssign, TokenType.BitXor }
            };

            _simplifyMap = new Dictionary<TokenType, Func<double, double, double>>
            {
                { TokenType.Add, (x, y) => x + y },
                { TokenType.Subtract, (x, y) => x - y },
                { TokenType.Multiply, (x, y) => x * y },
                { TokenType.Divide, (x, y) => x / y },
                { TokenType.Modulo, (x, y) => x % y },
                { TokenType.Exponent, Math.Pow },
                { TokenType.BitLeftShift, (x, y) => (int)x << (int)y },
                { TokenType.BitRightShift, (x, y) => (int)x >> (int)y },
                { TokenType.BitAnd, (x, y) => (int)x & (int)y },
                { TokenType.BitOr, (x, y) => (int)x | (int)y },
                { TokenType.BitXor, (x, y) => (int)x ^ (int)y }
            };
        }
    }
}

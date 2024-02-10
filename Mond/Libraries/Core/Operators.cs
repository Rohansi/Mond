using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Operators", bareMethods: true)]
    internal static partial class OperatorModule
    {
        [MondFunction("op_Dot")]
        public static MondValue Dot(MondValue x, MondValue y) => x[y];

        [MondFunction("op_Plus")]
        public static MondValue Add(MondValue x, MondValue y) => x + y;

        [MondFunction("op_Minus")]
        public static MondValue Subtract(MondValue x, MondValue y) => x - y;

        [MondFunction("op_Minus")]
        public static MondValue Neg(MondValue x) => -x;

        [MondFunction("op_Asterisk")]
        public static MondValue Multiply(MondValue x, MondValue y) => x * y;

        [MondFunction("op_Slash")]
        public static MondValue Divide(MondValue x, MondValue y) => x / y;

        [MondFunction("op_Percent")]
        public static MondValue Modulo(MondValue x, MondValue y) => x % y;

        [MondFunction("op_AsteriskAsterisk")]
        public static MondValue Exponent(MondValue x, MondValue y) => x.Pow(y);

        [MondFunction("op_Ampersand")]
        public static MondValue BitAnd(MondValue x, MondValue y) => x & y;

        [MondFunction("op_Pipe")]
        public static MondValue BitOr(MondValue x, MondValue y) => x | y;

        [MondFunction("op_Caret")]
        public static MondValue BitXor(MondValue x, MondValue y) => x ^ y;

        [MondFunction("op_Tilde")]
        public static MondValue BitNot(MondValue x) => ~x;

        [MondFunction("op_LeftAngleLeftAngle")]
        public static MondValue BitLeftShift(MondValue x, MondValue y) => x.LShift(y);

        [MondFunction("op_RightAngleRightAngle")]
        public static MondValue BitRightShift(MondValue x, MondValue y) => x.RShift(y);

        [MondFunction("op_PlusPlus")]
        public static MondValue Increment(MondValue x) => ++x;

        [MondFunction("op_MinusMinus")]
        public static MondValue Decrement(MondValue x) => --x;

        [MondFunction("op_EqualsEquals")]
        public static MondValue EqualTo(MondValue x, MondValue y) => x == y;

        [MondFunction("op_BangEquals")]
        public static MondValue NotEqualTo(MondValue x, MondValue y) => x != y;

        [MondFunction("op_RightAngle")]
        public static MondValue GreaterThan(MondValue x, MondValue y) => x > y;

        [MondFunction("op_RightAngleEquals")]
        public static MondValue GreaterThanOrEqual(MondValue x, MondValue y) => x >= y;

        [MondFunction("op_LeftAngle")]
        public static MondValue LessThan(MondValue x, MondValue y) => x < y;

        [MondFunction("op_LeftAngleEquals")]
        public static MondValue LessThanOrEqual(MondValue x, MondValue y) => x <= y;

        [MondFunction("op_Bang")]
        public static MondValue Not(MondValue x) => !x;

        [MondFunction("op_AmpersandAmpersand")]
        public static MondValue ConditionalAnd(MondValue x, MondValue y) => x && y;

        [MondFunction("op_PipePipe")]
        public static MondValue ConditionalOr(MondValue x, MondValue y) => x || y;
    }
}

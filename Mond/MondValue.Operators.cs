using System;
using System.Globalization;
using Mond.VirtualMachine;

namespace Mond
{
    public partial class MondValue
    {
        public static implicit operator MondValue(bool value)
        {
            return value ? True : False;
        }

        public static implicit operator MondValue(double value)
        {
            return new MondValue(value);
        }

        public static implicit operator MondValue(string value)
        {
            return new MondValue(value);
        }

        public static implicit operator MondValue(MondFunction function)
        {
            return new MondValue(new Closure(function));
        }

        public static implicit operator MondValue(MondInstanceFunction function)
        {
            return new MondValue(new Closure(function));
        }

        public static implicit operator bool(MondValue value)
        {
            switch (value.Type)
            {
                case MondValueType.Undefined:
                case MondValueType.Null:
                case MondValueType.False:
                    return false;

                case MondValueType.Number:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return value._numberValue != 0 && !double.IsNaN(value._numberValue);

                case MondValueType.Object:
                    {
                        MondValue result;
                        if (value.TryDispatch("__bool", out result, value))
                            return result;

                        return true;
                    }

                default:
                    return true;
            }
        }

        public static bool operator true(MondValue value)
        {
            return value;
        }

        public static bool operator false(MondValue value)
        {
            return !value;
        }

        public static implicit operator double(MondValue value)
        {
            if (value.Type != MondValueType.Number)
                throw new MondRuntimeException(RuntimeError.CantCastTo, value.Type, MondValueType.Number);

            return value._numberValue;
        }

        public static implicit operator string(MondValue value)
        {
            if (value.Type != MondValueType.String)
                throw new MondRuntimeException(RuntimeError.CantCastTo, value.Type, MondValueType.String);

            return value._stringValue;
        }

        public static MondValue operator +(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.String || right.Type == MondValueType.String)
                return new MondValue(left.ToString() + right.ToString());

            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(left._numberValue + right._numberValue);

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__add", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "addition", left.Type, right.Type);
        }

        public static MondValue operator -(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(left._numberValue - right._numberValue);

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__sub", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "subtraction", left.Type, right.Type);
        }

        public static MondValue operator *(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(left._numberValue * right._numberValue);

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__mul", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "multiplication", left.Type, right.Type);
        }

        public static MondValue operator /(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(left._numberValue / right._numberValue);

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__div", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "division", left.Type, right.Type);
        }

        public static MondValue operator %(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(left._numberValue % right._numberValue);

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__mod", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "modulo", left.Type, right.Type);
        }

        public static MondValue Pow(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(Math.Pow(left._numberValue, right._numberValue));

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__pow", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "exponent", left.Type, right.Type);
        }

        internal static MondValue LShift(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__lshift", out result, left, right))
                    return result;
            }

            return left << (int)right;
        }

        internal static MondValue RShift(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__rshift", out result, left, right))
                    return result;
            }

            return left >> (int)right;
        }

        public static MondValue operator <<(MondValue left, int right)
        {
            if (left.Type != MondValueType.Number)
                throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise left shift", left.Type, MondValueType.Number);

            return new MondValue(((int)left._numberValue) << right);
        }

        public static MondValue operator >>(MondValue left, int right)
        {
            if (left.Type != MondValueType.Number)
                throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise right shift", left.Type, MondValueType.Number);

            return new MondValue(((int)left._numberValue) >> right);
        }

        public static MondValue operator &(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(((int)left._numberValue) & ((int)right._numberValue));

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__and", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise and", left.Type, right.Type);
        }

        public static MondValue operator |(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(((int)left._numberValue) | ((int)right._numberValue));

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__or", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise or", left.Type, right.Type);
        }

        public static MondValue operator ^(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return new MondValue(((int)left._numberValue) ^ ((int)right._numberValue));

            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__xor", out result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise xor (exclusive or)", left.Type, right.Type);
        }

        public static MondValue operator -(MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(-value._numberValue);

            if (value.Type == MondValueType.Object)
            {
                MondValue result;
                if (value.TryDispatch("__neg", out result, value))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "negation", value.Type);
        }

        public static MondValue operator ~(MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(~((int)value._numberValue));

            if (value.Type == MondValueType.Object)
            {
                MondValue result;
                if (value.TryDispatch("__not", out result, value))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "bitwise not", value.Type);
        }

        public static bool operator ==(MondValue left, MondValue right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(MondValue left, MondValue right)
        {
            return !(left == right);
        }

        public static bool operator >(MondValue left, MondValue right)
        {
            switch (left.Type)
            {
                case MondValueType.Number:
                    if (right.Type != MondValueType.Number)
                        throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type, right.Type);

                    return left._numberValue > right._numberValue;

                case MondValueType.String:
                    if (right.Type != MondValueType.String)
                        throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type, right.Type);

                    return string.Compare(left._stringValue, right._stringValue, CultureInfo.InvariantCulture, CompareOptions.None) > 0;

                case MondValueType.Object:
                    MondValue result;
                    if (left.TryDispatch("__gt", out result, left, right))
                        return result;

                    goto default;

                default:
                    throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type, right.Type);
            }
        }

        public static bool operator >=(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__gte", out result, left, right))
                    return result;
            }

            return left > right || left == right;
        }

        public static bool operator <(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__lt", out result, left, right))
                    return result;
            }

            return !(left >= right);
        }

        public static bool operator <=(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                MondValue result;
                if (left.TryDispatch("__lte", out result, left, right))
                    return result;
            }

            return !(left > right);
        }
    }
}

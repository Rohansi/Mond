using System;
using Mond.VirtualMachine;

namespace Mond
{
    public partial struct MondValue
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
            if (ReferenceEquals(value, null))
                return Null;

            return new MondValue(value);
        }

        public static implicit operator MondValue(MondFunction function)
        {
            if (ReferenceEquals(function, null))
                return Null;

            return new MondValue(new Closure(function));
        }

        public static implicit operator MondValue(MondInstanceFunction function)
        {
            if (ReferenceEquals(function, null))
                return Null;

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
                    return !double.IsNaN(value._numberValue);

                case MondValueType.Object:
                    if (value.TryDispatch("__bool", out var result, value))
                    {
                        if (result.Type != MondValueType.True && result.Type != MondValueType.False)
                            throw new MondRuntimeException(RuntimeError.BoolCastWrongType);

                        return result.Type == MondValueType.True;
                    }

                    return true;

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
            if (value.Type == MondValueType.Number)
                return value._numberValue;

            if (value.TryDispatch("__number", out var result, value))
            {
                if (result.Type != MondValueType.Number)
                    throw new MondRuntimeException(RuntimeError.NumberCastWrongType);

                return result._numberValue;
            }

            throw new MondRuntimeException(RuntimeError.FailedCastToNumber, value.Type.GetName());
        }

        public static implicit operator string(MondValue value)
        {
            return value.ToString();
        }

        public static MondValue operator +(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.String || right.Type == MondValueType.String)
                return new MondValue(left.ToString() + right.ToString());

            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue + (double)right);

            if (left.TryDispatch("__add", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left + right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "addition", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator -(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue - (double)right);

            if (left.TryDispatch("__sub", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left - right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "subtraction", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator *(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue * (double)right);

            if (left.TryDispatch("__mul", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left * right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "multiplication", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator /(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue / (double)right);

            if (left.TryDispatch("__div", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left / right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "division", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator %(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue % (double)right);

            if (left.TryDispatch("__mod", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left % right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "modulo", left.Type.GetName(), right.Type.GetName());
        }

        public MondValue Pow(MondValue right)
        {
            if (Type == MondValueType.Number)
                return new MondValue(Math.Pow(_numberValue, right));

            if (TryDispatch("__pow", out var result, this, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue(Math.Pow(this, right._numberValue));

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "exponent", Type.GetName(), right.Type.GetName());
        }

        public MondValue LShift(MondValue right)
        {
            if (Type == MondValueType.Object)
            {
                if (TryDispatch("__lshift", out var result, this, right))
                    return result;

                return new MondValue((int)this << (int)right);
            }

            return this << (int)right;
        }

        public MondValue RShift(MondValue right)
        {
            if (Type == MondValueType.Object)
            {
                if (TryDispatch("__rshift", out var result, this, right))
                    return result;

                return new MondValue((int)this >> (int)right);
            }

            return this >> (int)right;
        }

        public static MondValue operator <<(MondValue left, int right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue << right);

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__lshift", out var result, left, right))
                    return result;

                return new MondValue((int)left << right);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise left shift", left.Type.GetName(), MondValueType.Number.GetName());
        }

        public static MondValue operator >>(MondValue left, int right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue >> right);

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__rshift", out var result, left, right))
                    return result;

                return new MondValue((int)left >> right);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise right shift", left.Type.GetName(), MondValueType.Number.GetName());
        }

        public static MondValue operator &(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue & (int)right);

            if (left.TryDispatch("__and", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left & (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise and", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator |(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue | (int)right);

            if (left.TryDispatch("__or", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left | (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise or", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator ^(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue ^ (int)right);

            if (left.TryDispatch("__xor", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left ^ (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise xor", left.Type.GetName(), right.Type.GetName());
        }

        public static MondValue operator -(MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(-value._numberValue);

            if (value.Type == MondValueType.Object)
            {
                if (value.TryDispatch("__neg", out var result, value))
                    return result;

                return new MondValue(-(double)value);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "negation", value.Type.GetName());
        }

        public static MondValue operator ~(MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(~(int)value._numberValue);

            if (value.Type == MondValueType.Object)
            {
                if (value.TryDispatch("__not", out var result, value))
                    return result;

                return new MondValue(~(int)value);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "bitwise not", value.Type.GetName());
        }

        public static bool operator ==(MondValue left, MondValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MondValue left, MondValue right)
        {
            if (ReferenceEquals(left, right))
                return false;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return true;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__neq", out var result, left, right))
                    return result;
            }

            return !left.Equals(right);
        }

        public static bool operator >(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue > right._numberValue;

            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) > 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__gt", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        public static bool operator >=(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue >= right._numberValue;

            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) >= 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__gte", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        public static bool operator <(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue < right._numberValue;

            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) < 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__lt", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        public static bool operator <=(MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue <= right._numberValue;

            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) <= 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__lte", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }
    }
}

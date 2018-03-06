using System;
using System.Runtime.CompilerServices;
using Mond.VirtualMachine;
using Closure = Mond.VirtualMachine.Closure;

namespace Mond
{
    public partial struct MondValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MondValue(bool value)
        {
            return value ? True : False;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MondValue(double value)
        {
            return new MondValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MondValue(string value)
        {
            if (ReferenceEquals(value, null))
                return Null;

            return new MondValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MondValue(MondFunction function)
        {
            if (ReferenceEquals(function, null))
                return Null;

            return new MondValue(new Closure(function));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MondValue(MondInstanceFunction function)
        {
            if (ReferenceEquals(function, null))
                return Null;

            return new MondValue(new Closure(function));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in MondValue value)
        {
            if (value.Type == MondValueType.True)
                return true;

            if (value.Type == MondValueType.False)
                return false;

            return ConvertBoolSlow(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ConvertBoolSlow(in MondValue value)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator true(in MondValue value)
        {
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator false(in MondValue value)
        {
            return !value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(in MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return value._numberValue;

            return ConvertDoubleSlow(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double ConvertDoubleSlow(in MondValue value)
        {
            if (value.TryDispatch("__number", out var result, value))
            {
                if (result.Type != MondValueType.Number)
                    throw new MondRuntimeException(RuntimeError.NumberCastWrongType);

                return result._numberValue;
            }

            throw new MondRuntimeException(RuntimeError.FailedCastToNumber, value.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(in MondValue value)
        {
            return value.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator +(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.String || right.Type == MondValueType.String)
                return new MondValue(left.ToString() + right.ToString());

            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue + (double)right);

            return AddSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue AddSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__add", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left + right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "addition", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator -(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue - (double)right);

            return SubtractSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue SubtractSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__sub", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left - right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "subtraction", left.Type.GetName(), right.Type.GetName());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator *(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue * (double)right);

            return MultiplySlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue MultiplySlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__mul", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left * right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "multiplication", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator /(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue / (double)right);

            return DivideSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue DivideSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__div", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left / right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "division", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator %(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue(left._numberValue % (double)right);

            return ModuloSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue ModuloSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__mod", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((double)left % right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "modulo", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MondValue Pow(in MondValue right)
        {
            if (Type == MondValueType.Number)
                return new MondValue(Math.Pow(_numberValue, right));

            return PowSlow(right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private MondValue PowSlow(in MondValue right)
        {
            if (TryDispatch("__pow", out var result, this, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue(Math.Pow(this, right._numberValue));

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "exponent", Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MondValue LShift(in MondValue right)
        {
            if (Type == MondValueType.Object)
            {
                if (TryDispatch("__lshift", out var result, this, right))
                    return result;

                return new MondValue((int)this << (int)right);
            }

            return this << (int)right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MondValue RShift(in MondValue right)
        {
            if (Type == MondValueType.Object)
            {
                if (TryDispatch("__rshift", out var result, this, right))
                    return result;

                return new MondValue((int)this >> (int)right);
            }

            return this >> (int)right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator <<(in MondValue left, int right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue << right);

            return LShiftSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue LShiftSlow(in MondValue left, int right)
        {
            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__lshift", out var result, left, right))
                    return result;

                return new MondValue((int)left << right);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise left shift", left.Type.GetName(), MondValueType.Number.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator >>(in MondValue left, int right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue >> right);

            return RShiftSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MondValue RShiftSlow(in MondValue left, int right)
        {
            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__rshift", out var result, left, right))
                    return result;

                return new MondValue((int)left >> right);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise right shift", left.Type.GetName(), MondValueType.Number.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator &(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue & (int)right);

            return AndSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue AndSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__and", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left & (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise and", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator |(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue | (int)right);

            return OrSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue OrSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__or", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left | (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise or", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator ^(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number)
                return new MondValue((int)left._numberValue ^ (int)right);

            return XorSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue XorSlow(in MondValue left, in MondValue right)
        {
            if (left.TryDispatch("__xor", out var result, left, right))
                return result;

            if (right.Type == MondValueType.Number)
                return new MondValue((int)left ^ (int)right._numberValue);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "bitwise xor", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator -(in MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(-value._numberValue);

            return NegateSlow(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue NegateSlow(in MondValue value)
        {
            if (value.Type == MondValueType.Object)
            {
                if (value.TryDispatch("__neg", out var result, value))
                    return result;

                return new MondValue(-(double)value);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "negation", value.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MondValue operator ~(in MondValue value)
        {
            if (value.Type == MondValueType.Number)
                return new MondValue(~(int)value._numberValue);

            return NotSlow(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MondValue NotSlow(in MondValue value)
        {
            if (value.Type == MondValueType.Object)
            {
                if (value.TryDispatch("__not", out var result, value))
                    return result;

                return new MondValue(~(int)value);
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnType, "bitwise not", value.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in MondValue left, in MondValue right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__neq", out var result, left, right))
                    return result;
            }

            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue > right._numberValue;

            return GreaterThanSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool GreaterThanSlow(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) > 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__gt", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue >= right._numberValue;

            return GreaterThanEqualSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool GreaterThanEqualSlow(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) >= 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__gte", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue < right._numberValue;

            return LessThanSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LessThanSlow(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.String && right.Type == MondValueType.String)
                return string.Compare(left._stringValue, right._stringValue, StringComparison.Ordinal) < 0;

            if (left.Type == MondValueType.Object)
            {
                if (left.TryDispatch("__lt", out var result, left, right))
                    return result;
            }

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "relational", left.Type.GetName(), right.Type.GetName());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(in MondValue left, in MondValue right)
        {
            if (left.Type == MondValueType.Number && right.Type == MondValueType.Number)
                return left._numberValue <= right._numberValue;

            return LessThanEqualSlow(left, right);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LessThanEqualSlow(in MondValue left, in MondValue right)
        {
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

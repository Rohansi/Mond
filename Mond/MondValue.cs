using System;
using System.Collections.Generic;
using Mond.VirtualMachine;
using Mond.VirtualMachine.Prototypes;

namespace Mond
{
    public enum MondValueType
    {
        Undefined, Null, True, False, Object, Array, Number, String, Closure
    }

    public partial struct MondValue : IEquatable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);

        public readonly MondValueType Type;

        public readonly Dictionary<MondValue, MondValue> ObjectValue;
        public readonly List<MondValue> ArrayValue;
        public readonly double NumberValue;
        public readonly string StringValue;

        internal readonly Closure ClosureValue;

        public MondValue(MondValueType type)
        {
            Type = type;
            ObjectValue = null;
            ArrayValue = null;
            NumberValue = 0;
            StringValue = null;
            ClosureValue = null;

            switch (type)
            {
                case MondValueType.Undefined:
                case MondValueType.Null:
                case MondValueType.True:
                case MondValueType.False:
                    break;

                case MondValueType.Object:
                    ObjectValue = new Dictionary<MondValue, MondValue>();
                    break;

                case MondValueType.Array:
                    ArrayValue = new List<MondValue>();
                    break;

                default:
                    throw new MondException("Incorrect MondValue constructor use");
            }
        }

        public MondValue(double value)
        {
            Type = MondValueType.Number;
            NumberValue = value;

            ObjectValue = null;
            ArrayValue = null;
            StringValue = null;
            ClosureValue = null;
        }

        public MondValue(string value)
        {
            Type = MondValueType.String;
            StringValue = value;

            ObjectValue = null;
            ArrayValue = null;
            NumberValue = 0;
            ClosureValue = null;
        }

        public MondValue(MondFunction function)
        {
            Type = MondValueType.Closure;
            ClosureValue = new Closure(function);

            ObjectValue = null;
            ArrayValue = null;
            NumberValue = 0;
            StringValue = null;
        }

        public MondValue(MondInstanceFunction function)
        {
            Type = MondValueType.Closure;
            ClosureValue = new Closure(function);

            ObjectValue = null;
            ArrayValue = null;
            NumberValue = 0;
            StringValue = null;
        }

        internal MondValue(Closure closure)
        {
            Type = MondValueType.Closure;
            ClosureValue = closure;

            ObjectValue = null;
            ArrayValue = null;
            NumberValue = 0;
            StringValue = null;
        }

        public MondValue this[MondValue index]
        {
            get
            {
                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index.NumberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    return ArrayValue[n];
                }

                if (index == "prototype")
                {
                    var prototypeValue = GetPrototype();
                    if (prototypeValue.HasValue)
                        return CheckWrapInstanceNative(prototypeValue.Value);
                    return Undefined;
                }

                if (Type == MondValueType.Object)
                {
                    MondValue indexValue;
                    if (ObjectValue.TryGetValue(index, out indexValue))
                        return CheckWrapInstanceNative(indexValue);
                }

                var i = 0;
                var prototype = GetPrototype();
                
                while (prototype.HasValue)
                {
                    var currentValue = prototype.Value;

                    if (currentValue.Type != MondValueType.Object)
                        break;
                    
                    MondValue indexValue;
                    if (currentValue.ObjectValue.TryGetValue(index, out indexValue))
                        return CheckWrapInstanceNative(indexValue);
                    
                    prototype = currentValue.GetPrototype();
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                return Undefined;
            }
            set
            {
                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index.NumberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    ArrayValue[n] = value;
                    return;
                }

                if (index == "prototype")
                {
                    if (Type != MondValueType.Object)
                        throw new MondRuntimeException(RuntimeError.CantCreateField, Type);

                    ObjectValue["prototype"] = value;
                    return;
                }

                if (Type == MondValueType.Object)
                {
                    if (ObjectValue.ContainsKey(index))
                    {
                        ObjectValue[index] = value;
                        return;
                    }
                }

                var i = 0;
                var prototype = GetPrototype();

                while (prototype.HasValue)
                {
                    var currentValue = prototype.Value;

                    if (currentValue.Type != MondValueType.Object)
                        break;

                    if (currentValue.ObjectValue.ContainsKey(index))
                    {
                        currentValue.ObjectValue[index] = value;
                        return;
                    }

                    prototype = currentValue.GetPrototype();
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type != MondValueType.Object)
                    throw new MondRuntimeException(RuntimeError.CantCreateField, Type);

                ObjectValue[index] = value;
            }
        }

        public bool Equals(MondValue other)
        {
            if (Type != other.Type)
                return false;

            switch (Type)
            {
                case MondValueType.Object:
                    return ReferenceEquals(ObjectValue, other.ObjectValue);

                case MondValueType.Array:
                    return ReferenceEquals(ArrayValue, other.ArrayValue);

                case MondValueType.Number:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return NumberValue == other.NumberValue;

                case MondValueType.String:
                    return StringValue == other.StringValue;

                case MondValueType.Closure:
                    return ReferenceEquals(ClosureValue, other.ClosureValue);

                default:
                    return Type == other.Type;
            }
        }

        public override bool Equals(object other)
        {
            if (!(other is MondValue))
                return false;

            return Equals((MondValue)other);
        }

        public override int GetHashCode()
        {
            switch (Type)
            {
                case MondValueType.Undefined:
                    return int.MinValue;

                case MondValueType.Null:
                    return int.MaxValue;

                case MondValueType.True:
                    return 1;

                case MondValueType.False:
                    return 0;

                case MondValueType.Object:
                    return ObjectValue.GetHashCode();

                case MondValueType.Array:
                    return ArrayValue.GetHashCode();

                case MondValueType.Number:
                    return NumberValue.GetHashCode();

                case MondValueType.String:
                    return StringValue.GetHashCode();

                case MondValueType.Closure:
                    return ClosureValue.GetHashCode();
            }

            throw new NotSupportedException();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case MondValueType.Undefined:
                    return "undefined";
                case MondValueType.Null:
                    return "null";
                case MondValueType.True:
                    return "true";
                case MondValueType.False:
                    return "false";
                case MondValueType.Object:
                    return "object";
                case MondValueType.Array:
                    return "array";
                case MondValueType.Number:
                    return string.Format("{0:R}", NumberValue);
                case MondValueType.String:
                    return StringValue;
                case MondValueType.Closure:
                    return "closure";
                default:
                    throw new NotSupportedException();
            }
        }

        private MondValue? GetPrototype()
        {
            switch (Type)
            {
                case MondValueType.Object:
                    MondValue prototype;
                    if (ObjectValue.TryGetValue("prototype", out prototype))
                    {
                        if (!prototype)
                            return null;

                        return prototype;
                    }

                    return ObjectPrototype.Value;

                case MondValueType.Array:
                    return ArrayPrototype.Value;

                default:
                    return null; // TODO: provide prototypes for other types
            }
        }

        private MondValue CheckWrapInstanceNative(MondValue value)
        {
            if (value.Type != MondValueType.Closure || value.ClosureValue.Type != ClosureType.InstanceNative)
                return value;

            var func = value.ClosureValue.InstanceNativeFunction;
            var inst = this;
            return new MondValue((state, args) => func(state, inst, args));
        }
    }
}

using System;
using System.Collections.Generic;
using Mond.Prototypes;
using Mond.VirtualMachine;

namespace Mond
{
    public enum MondValueType
    {
        Undefined, Null, True, False, Object, Array, Number, String, Function
    }

    public partial class MondValue : IEquatable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);

        public readonly MondValueType Type;

        internal readonly MondObject ObjectValue;
        internal readonly List<MondValue> ArrayValue;

        private readonly double _numberValue;
        private readonly string _stringValue;

        internal readonly Closure FunctionValue;

        private MondValue()
        {
            Type = MondValueType.Undefined;

            ObjectValue = null;
            ArrayValue = null;
            _numberValue = 0;
            _stringValue = null;

            FunctionValue = null;
        }

        /// <summary>
        /// Construct a new MondValue. Should only be used for Object or Array.
        /// </summary>
        public MondValue(MondValueType type)
            : this()
        {
            Type = type;

            switch (type)
            {
                case MondValueType.Undefined:
                case MondValueType.Null:
                case MondValueType.True:
                case MondValueType.False:
                    break;

                case MondValueType.Object:
                    ObjectValue = new MondObject();
                    break;

                case MondValueType.Array:
                    ArrayValue = new List<MondValue>();
                    break;

                default:
                    throw new MondException("Incorrect MondValue constructor use");
            }
        }

        /// <summary>
        /// Construct a new Number MondValue with the specified value.
        /// </summary>
        public MondValue(double value)
            : this()
        {
            Type = MondValueType.Number;
            _numberValue = value;
        }

        /// <summary>
        /// Construct a new String MondValue with the specified value.
        /// </summary>
        public MondValue(string value)
            : this()
        {
            Type = MondValueType.String;
            _stringValue = value;
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value.
        /// </summary>
        public MondValue(MondFunction function)
            : this()
        {
            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value. Instance functions will
        /// bind themselves to their parent object when being retrieved.
        /// </summary>
        public MondValue(MondInstanceFunction function)
            : this()
        {
            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
        }

        internal MondValue(Closure closure)
            : this()
        {
            Type = MondValueType.Function;
            FunctionValue = closure;
        }

        /// <summary>
        /// Get or set values in the Object or Array or its' prototype.
        /// </summary>
        public MondValue this[MondValue index]
        {
            get
            {
                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index._numberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    return ArrayValue[n];
                }

                if (Type == MondValueType.Object)
                {
                    MondValue indexValue;
                    if (ObjectValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapInstanceNative(indexValue);
                }

                var i = 0;
                var prototype = Prototype;

                while (prototype != null)
                {
                    var currentValue = prototype;

                    if (currentValue.Type != MondValueType.Object)
                        break;
                    
                    MondValue indexValue;
                    if (currentValue.ObjectValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapInstanceNative(indexValue);
                    
                    prototype = currentValue.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                return Undefined;
            }
            set
            {
                if (index == null)
                    throw new ArgumentNullException("index");

                if (value == null)
                    throw new ArgumentNullException("value");

                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index._numberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    ArrayValue[n] = value;
                    return;
                }

                if (Type == MondValueType.Object && !ObjectValue.Locked)
                {
                    if (ObjectValue.Values.ContainsKey(index))
                    {
                        ObjectValue.Values[index] = value;
                        return;
                    }
                }

                var i = 0;
                var prototype = Prototype;

                while (prototype != null)
                {
                    var currentValue = prototype;

                    if (currentValue.Type != MondValueType.Object)
                        break;

                    // skip locked objects because they cant be written to
                    if (!currentValue.ObjectValue.Locked)
                    {
                        var values = currentValue.ObjectValue.Values;
                        if (values.ContainsKey(index))
                        {
                            values[index] = value;
                            return;
                        }
                    }

                    prototype = currentValue.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type != MondValueType.Object)
                    throw new MondRuntimeException(RuntimeError.CantCreateField, Type);

                ObjectValue.Values[index] = value;
            }
        }

        /// <summary>
        /// Locks an Object to prevent modification from scripts. All prototypes should be locked.
        /// </summary>
        public void Lock()
        {
            if (Type != MondValueType.Object)
                throw new MondRuntimeException("Attempt to lock non-object");

            ObjectValue.Locked = true;
        }

        /// <summary>
        /// Gets the prototype object for this value.
        /// 
        /// Sets the prototype object for this object. Can either be MondValueType.Object, MondValue.Null, MondValue.Undefined or null.
        /// If set to MondValue.Undefined or null, the default prototype will be used.
        /// </summary>
        public MondValue Prototype
        {
            get
            {
                switch (Type)
                {
                    case MondValueType.Object:
                        return ObjectValue.Prototype ?? ObjectPrototype.Value;

                    case MondValueType.Array:
                        return ArrayPrototype.Value;

                    case MondValueType.String:
                        return StringPrototype.Value;

                    default:
                        return ValuePrototype.Value;
                }
            }
            set
            {
                if (Type != MondValueType.Object)
                    throw new Exception("Prototypes can only be set on objects");

                if (value == Undefined)
                    value = null;

                if (value != null && value.Type != MondValueType.Object && value.Type != MondValueType.Null)
                    throw new Exception("Prototypes must be an object or null");

                ObjectValue.Prototype = value;
            }
        }

        public bool Equals(MondValue other)
        {
            if (ReferenceEquals(other, null))
                return false;

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
                    return _numberValue == other._numberValue;

                case MondValueType.String:
                    return _stringValue == other._stringValue;

                case MondValueType.Function:
                    return ReferenceEquals(FunctionValue, other.FunctionValue);

                default:
                    return Type == other.Type;
            }
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, null))
                return false;

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
                    return _numberValue.GetHashCode();

                case MondValueType.String:
                    return _stringValue.GetHashCode();

                case MondValueType.Function:
                    return FunctionValue.GetHashCode();
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
                    return string.Format("{0:R}", _numberValue);
                case MondValueType.String:
                    return _stringValue;
                case MondValueType.Function:
                    return "function";
                default:
                    throw new NotSupportedException();
            }
        }

        private MondValue CheckWrapInstanceNative(MondValue value)
        {
            if (value.Type != MondValueType.Function || value.FunctionValue.Type != ClosureType.InstanceNative)
                return value;

            var func = value.FunctionValue.InstanceNativeFunction;
            var inst = this;
            return new MondValue((state, args) => func(state, inst, args));
        }
    }
}

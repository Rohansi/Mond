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

    public partial class MondValue : IEquatable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);

        public readonly MondValueType Type;

        internal readonly Dictionary<MondValue, MondValue> ObjectValue;
        private bool _objectLocked;

        internal readonly List<MondValue> ArrayValue;

        private readonly double _numberValue;
        private readonly string _stringValue;

        internal readonly Closure ClosureValue;

        private MondValue()
        {
            Type = MondValueType.Undefined;

            ObjectValue = null;
            _objectLocked = false;

            ArrayValue = null;
            _numberValue = 0;
            _stringValue = null;

            ClosureValue = null;
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
                    ObjectValue = new Dictionary<MondValue, MondValue>();
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
        /// Construct a new Closure MondValue with the specified value.
        /// </summary>
        public MondValue(MondFunction function)
            : this()
        {
            Type = MondValueType.Closure;
            ClosureValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Closure MondValue with the specified value. Instance closures will bind
        /// themselves to their parent object when being retrieved.
        /// </summary>
        public MondValue(MondInstanceFunction function)
            : this()
        {
            Type = MondValueType.Closure;
            ClosureValue = new Closure(function);
        }

        internal MondValue(Closure closure)
            : this()
        {
            Type = MondValueType.Closure;
            ClosureValue = closure;
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

                if (index == "prototype")
                {
                    var prototypeValue = GetPrototype();
                    if (prototypeValue != null)
                        return CheckWrapInstanceNative(prototypeValue);
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

                while (prototype != null)
                {
                    var currentValue = prototype;

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
                if (index == null)
                    throw new ArgumentNullException("index");

                if (value == null)
                    throw new ArgumentNullException("value");

                if (Type == MondValueType.Object && _objectLocked)
                    return;

                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index._numberValue;

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

                while (prototype != null)
                {
                    var currentValue = prototype;

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

        /// <summary>
        /// Locks an Object to prevent modification from scripts. All prototypes should be locked.
        /// </summary>
        public void Lock()
        {
            _objectLocked = true;
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

                case MondValueType.Closure:
                    return ReferenceEquals(ClosureValue, other.ClosureValue);

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
                    return string.Format("{0:R}", _numberValue);
                case MondValueType.String:
                    return _stringValue;
                case MondValueType.Closure:
                    return "closure";
                default:
                    throw new NotSupportedException();
            }
        }

        private MondValue GetPrototype()
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
                    return ObjectPrototype.Value; // TODO: provide proper prototypes for other types
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

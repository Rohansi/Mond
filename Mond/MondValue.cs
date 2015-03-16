using System;
using System.Collections.Generic;
using Mond.VirtualMachine;
using Mond.VirtualMachine.Prototypes;

#if !UNITY
using System.Runtime.InteropServices;
#endif

namespace Mond
{
#if !UNITY
    [StructLayout(LayoutKind.Explicit)]
#endif
    public sealed partial class MondValue : IEquatable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);
        
#if !UNITY
        [FieldOffset(0)]
#endif
        public readonly MondValueType Type;
        
#if !UNITY
        [FieldOffset(8)]
#endif
        private readonly double _numberValue;
        
#if !UNITY
        [FieldOffset(16)]
#endif
        internal readonly VirtualMachine.Object ObjectValue;
        
#if !UNITY
        [FieldOffset(16)]
#endif
        internal readonly List<MondValue> ArrayValue;
        
#if !UNITY
        [FieldOffset(16)]
#endif
        private readonly string _stringValue;
        
#if !UNITY
        [FieldOffset(16)]
#endif
        internal readonly Closure FunctionValue;

        /// <summary>
        /// Construct a new MondValue. Should only be used for Object or Array.
        /// </summary>
        public MondValue(MondValueType type)
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
                    ObjectValue = new VirtualMachine.Object();
                    break;

                case MondValueType.Array:
                    ArrayValue = new List<MondValue>();
                    break;

                default:
                    throw new MondException("Incorrect MondValue constructor use");
            }
        }

        /// <summary>
        /// Construct a new Object MondValue and attach a MondState to it. Should be used if using metamethods.
        /// </summary>
        public MondValue(MondState state)
        {
            Type = MondValueType.Object;
            ObjectValue = new VirtualMachine.Object();
            ObjectValue.State = state;
        }

        /// <summary>
        /// Construct a new Number MondValue with the specified value.
        /// </summary>
        public MondValue(double value)
        {
            Type = MondValueType.Number;
            _numberValue = value;
        }

        /// <summary>
        /// Construct a new String MondValue with the specified value.
        /// </summary>
        public MondValue(string value)
        {
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException("value");

            Type = MondValueType.String;
            _stringValue = value;
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value.
        /// </summary>
        public MondValue(MondFunction function)
        {
            if (ReferenceEquals(function, null))
                throw new ArgumentNullException("function");

            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value. Instance functions will
        /// bind themselves to their parent object when being retrieved.
        /// </summary>
        public MondValue(MondInstanceFunction function)
        {
            if (ReferenceEquals(function, null))
                throw new ArgumentNullException("function");

            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Array MondValue with the specified values.
        /// </summary>
        public MondValue(IEnumerable<MondValue> values)
        {
            if (ReferenceEquals(values, null))
                throw new ArgumentNullException("values");

            Type = MondValueType.Array;
            ArrayValue = new List<MondValue>(values);
        }

        /// <summary>
        /// Construct a new Object MondValue with the specified values.
        /// </summary>
        public MondValue(IEnumerable<KeyValuePair<MondValue, MondValue>> values)
        {
            Type = MondValueType.Object;
            ObjectValue = new VirtualMachine.Object();

            var obj = Object;
            foreach (var kvp in values)
            {
                obj.Add(kvp.Key, kvp.Value);
            }
        }

        internal MondValue(Closure closure)
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
                if (ReferenceEquals(index, null))
                    throw new ArgumentNullException("index");

                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index._numberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    return ArrayValue[n];
                }

                MondValue indexValue;
                if (Type == MondValueType.Object)
                {
                    if (ObjectValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapFunction(indexValue, ObjectValue.ThisEnabled);
                }

                var i = 0;
                var prototype = Prototype;

                while (prototype != null)
                {
                    var currentValue = prototype;

                    if (currentValue.Type != MondValueType.Object)
                        break;

                    var currentObjValue = currentValue.ObjectValue;
                    if (currentObjValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapFunction(indexValue, currentObjValue.ThisEnabled);

                    prototype = currentValue.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type == MondValueType.Object)
                {
                    if (TryDispatch("__get", out indexValue, this, index))
                        return CheckWrapFunction(indexValue, ObjectValue.ThisEnabled);
                }

                return Undefined;
            }
            set
            {
                if (ReferenceEquals(index, null))
                    throw new ArgumentNullException("index");

                if (ReferenceEquals(value, null))
                    throw new ArgumentNullException("value");

                if (Type == MondValueType.Array && index.Type == MondValueType.Number)
                {
                    var n = (int)index._numberValue;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    ArrayValue[n] = value;
                    return;
                }

                if (Type == MondValueType.Object && ObjectValue.Values.ContainsKey(index))
                {
                    if (ObjectValue.Locked)
                        throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

                    ObjectValue.Values[index] = value;
                    return;
                }

                var i = 0;
                var prototype = Prototype;

                while (prototype != null)
                {
                    var currentValue = prototype;

                    if (currentValue.Type != MondValueType.Object)
                        break;

                    var values = currentValue.ObjectValue.Values;
                    if (values.ContainsKey(index))
                    {
                        if (currentValue.ObjectValue.Locked)
                            break; // hit a wall in the prototype chain, don't continue

                        values[index] = value;
                        return;
                    }

                    prototype = currentValue.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type != MondValueType.Object)
                    throw new MondRuntimeException(RuntimeError.CantCreateField, Type.GetName());

                if (ObjectValue.Locked)
                    throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

                MondValue result;
                if (TryDispatch("__set", out result, this, index, value))
                    return;

                ObjectValue.Values[index] = value;
            }
        }

        /// <summary>
        /// Gets the dictionary instance used to store this object's values.
        /// </summary>
        public IDictionary<MondValue, MondValue> Object
        {
            get
            {
                if (Type != MondValueType.Object)
                    throw new InvalidOperationException("MondValue.Object is only valid on objects");

                return ObjectValue.Values;
            }
        }

        /// <summary>
        /// Gets the list instance used to store this array's values.
        /// </summary>
        public IList<MondValue> Array
        {
            get
            {
                if (Type != MondValueType.Array)
                    throw new InvalidOperationException("MondValue.Array is only valid on arrays");

                return ArrayValue;
            }
        } 

        /// <summary>
        /// Gets the prototype object for this value.
        /// 
        /// <para>
        /// Sets the prototype object for this object. If set to MondValue.Undefined
        /// or null, the default prototype will be used. If set to MondValue.Null,
        /// ValuePrototype will be used.
        /// </para>
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

                    case MondValueType.Number:
                        return NumberPrototype.Value;

                    case MondValueType.String:
                        return StringPrototype.Value;

                    default:
                        return ValuePrototype.Value;
                }
            }
            set
            {
                if (Type != MondValueType.Object)
                    throw new MondRuntimeException("Prototypes can only be set on objects");

                if (ObjectValue.Locked)
                    throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

                if (value == Undefined)
                    value = null;
                else if (value == Null)
                    value = ValuePrototype.Value;

                if (value != null && value.Type != MondValueType.Object && value.Type != MondValueType.Null)
                    throw new MondRuntimeException("Prototypes must be an object or null");

                ObjectValue.Prototype = value;
            }
        }

        /// <summary>
        /// Gets or sets the user data value of an Object.
        /// </summary>
        public object UserData
        {
            get
            {
                if (Type != MondValueType.Object)
                    throw new MondRuntimeException("UserData is only available on Objects");

                return ObjectValue.UserData;
            }
            set
            {
                if (Type != MondValueType.Object)
                    throw new MondRuntimeException("UserData is only available on Objects");

                ObjectValue.UserData = value;
            }
        }

        /// <summary>
        /// Locks an Object to prevent modification from scripts. All prototypes should be locked.
        /// </summary>
        public void Lock()
        {
            if (Type != MondValueType.Object)
                throw new MondRuntimeException("Cannot lock non-object");

            ObjectValue.Locked = true;
        }

        public bool IsLocked
        {
            get { return Type == MondValueType.Object && ObjectValue.Locked; }
        }

        public void EnableThis()
        {
            if (Type != MondValueType.Object)
                throw new MondRuntimeException("Cannot enable this on non-object");

            if (ObjectValue.Locked)
                throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

            ObjectValue.ThisEnabled = true;
        }

        public bool Contains(MondValue search)
        {
            if (Type == MondValueType.String && search.Type == MondValueType.String)
                return _stringValue.Contains(search._stringValue);

            if (Type == MondValueType.Object)
            {
                if (ObjectValue.Values.ContainsKey(search))
                    return true;

                MondValue result;
                if (TryDispatch("__in", out result, this, search))
                    return result;

                return false;
            }

            if (Type == MondValueType.Array)
                return ArrayValue.Contains(search);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "in", Type.GetName(), search.Type.GetName());
        }

        public MondValue Slice(MondValue start = null, MondValue end = null, MondValue step = null)
        {
            if (Type != MondValueType.Array)
                throw new MondRuntimeException("Slices can only be created from arrays");

            Func<MondValue, int, int> toIntOrDefault = (value, defaultValue) =>
            {
                if (value == null || !value)
                    return defaultValue;

                return (int)value;
            };

            // get start value
            var startIndex = toIntOrDefault(start, 0);

            if (startIndex < 0)
                startIndex += ArrayValue.Count;

            if (startIndex < 0 || (startIndex >= ArrayValue.Count && ArrayValue.Count > 0))
                throw new MondRuntimeException("Slice start index out of bounds");

            // get end value
            var endIndex = toIntOrDefault(end, Math.Max(0, ArrayValue.Count - 1));

            if (endIndex < 0)
                endIndex += ArrayValue.Count;

            if (endIndex < 0 || (endIndex >= ArrayValue.Count && ArrayValue.Count > 0))
                throw new MondRuntimeException("Slice end index out of bounds");

            // get step value
            var stepValue = toIntOrDefault(step, startIndex <= endIndex ? 1 : -1);

            if (stepValue == 0)
                throw new MondRuntimeException("Slice step value must be non-zero");

            // allow reversing with default indices, ex: [::-1]
            if (stepValue < 0 && (start == null || !start) && (end == null || !end))
            {
                startIndex = Math.Max(0, ArrayValue.Count - 1);
                endIndex = 0;
            }

            // make sure the range makes sense
            if ((stepValue < 0 && endIndex > startIndex) || (stepValue > 0 && startIndex > endIndex))
                throw new MondRuntimeException("Slice range is invalid"); // TODO: better error message

            // find size of slice
            int length;

            if (ArrayValue.Count == 0 && startIndex == 0 && endIndex == 0)
            {
                length = 0; // allow cloning empty arrays
            }
            else
            {
                var range = endIndex - startIndex + Math.Sign(stepValue);
                length = (range / stepValue) + (range % stepValue != 0 ? 1 : 0);
            }

            // copy values to new array
            var result = new MondValue(MondValueType.Array);
            result.ArrayValue.Capacity = length;

            var src = startIndex;
            for (var dst = 0; dst < length; src += stepValue, dst++)
            {
                result.ArrayValue.Add(ArrayValue[src]);
            }

            return result;
        }

        public bool Equals(MondValue other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Type == MondValueType.Object || other.Type == MondValueType.Object)
            {
                if (ReferenceEquals(ObjectValue, other.ObjectValue))
                    return true;

                MondValue result;
                if (TryDispatch("__eq", out result, this, other))
                    return result;

                if (other.TryDispatch("__eq", out result, other, this))
                    return result;

                return false;
            }

            switch (Type)
            {
                case MondValueType.Array:
                    return other.Type == MondValueType.Array && ReferenceEquals(ArrayValue, other.ArrayValue);

                case MondValueType.Number:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return other.Type == MondValueType.Number && _numberValue == other._numberValue;

                case MondValueType.String:
                    return other.Type == MondValueType.String && _stringValue == other._stringValue;

                case MondValueType.Function:
                    return other.Type == MondValueType.Function && ReferenceEquals(FunctionValue, other.FunctionValue);

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
                case MondValueType.True:
                    return "true";
                case MondValueType.False:
                    return "false";
                case MondValueType.Object:
                    {
                        MondValue result;
                        if (TryDispatch("__string", out result, this))
                        {
                            if (result.Type != MondValueType.String)
                                throw new MondRuntimeException(RuntimeError.StringCastWrongType);

                            return result._stringValue;
                        }

                        return "object";
                    }
                case MondValueType.Number:
                    return string.Format("{0:R}", _numberValue);
                case MondValueType.String:
                    return _stringValue;
                default:
                    return Type.GetName();
            }
        }

        private MondValue CheckWrapFunction(MondValue value, bool thisEnabled)
        {
            if (value.Type != MondValueType.Function)
                return value;

            switch (value.FunctionValue.Type)
            {
                case ClosureType.Mond:
                {
                    if (!thisEnabled)
                        break;

                    var func = value;
                    var inst = this;
                    return new MondValue((state, args) =>
                    {
                        // insert "this" value into argument array
                        System.Array.Resize(ref args, args.Length + 1);
                        System.Array.Copy(args, 0, args, 1, args.Length - 1);
                        args[0] = inst;

                        return state.Call(func, args);
                    });
                }

                case ClosureType.InstanceNative:
                {
                    var func = value.FunctionValue.InstanceNativeFunction;
                    var inst = this;
                    return new MondValue((state, args) => func(state, inst, args));
                }
            }
            
            return value;
        }

        // Convenience function on VirtualMachine.Object.TryDispatch
        private bool TryDispatch(string name, out MondValue result, params MondValue[] args)
        {
            if (Type == MondValueType.Object)
                return ObjectValue.TryDispatch(name, out result, args);

            result = Undefined;
            return false;
        }

        private static bool TryDispatchBinary(string name, out MondValue result, MondValue left, MondValue right)
        {
            if (left.Type == MondValueType.Object)
                return left.ObjectValue.TryDispatch(name, out result, left, right, false);

            if (right.Type == MondValueType.Object)
                return right.ObjectValue.TryDispatch(name, out result, left, right, true);

            result = Undefined;
            return false;
        }
    }
}

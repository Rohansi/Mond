using System;
using System.Collections.Generic;
using Mond.VirtualMachine;
using Mond.VirtualMachine.Prototypes;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Closure = Mond.VirtualMachine.Closure;

namespace Mond
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly partial struct MondValue : IEquatable<MondValue>, IComparable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);

        [FieldOffset(0)]
        private readonly MondValueType _type;

        [FieldOffset(8)]
        private readonly double _numberValue;
        
        [FieldOffset(16)]
        internal readonly VirtualMachine.Object ObjectValue;
        
        [FieldOffset(16)]
        internal readonly List<MondValue> ArrayValue;
        
        [FieldOffset(16)]
        private readonly string _stringValue;
        
        [FieldOffset(16)]
        internal readonly Closure FunctionValue;

        /// <summary>
        /// Construct a new MondValue. Should only be used for Object or Array.
        /// </summary>
        private MondValue(MondValueType type)
        {
            _type = type;
            _numberValue = 0;

            switch (type)
            {
                case MondValueType.Undefined:
                case MondValueType.Null:
                case MondValueType.True:
                case MondValueType.False:
                    ObjectValue = null;
                    ArrayValue = null;
                    _stringValue = null;
                    FunctionValue = null;
                    break;

                case MondValueType.Object:
                    ArrayValue = null;
                    _stringValue = null;
                    FunctionValue = null;
                    ObjectValue = new VirtualMachine.Object();
                    break;

                case MondValueType.Array:
                    ObjectValue = null;
                    _stringValue = null;
                    FunctionValue = null;
                    ArrayValue = new List<MondValue>();
                    break;

                default:
                    throw new MondException("Incorrect MondValue constructor use");
            }
        }

        /// <summary>
        /// Construct a new Object MondValue and attach a MondState to it. Should be used if using metamethods.
        /// </summary>
        private MondValue(MondState state)
            : this(MondValueType.Object)
        {
            ObjectValue.State = state;
        }

        /// <summary>
        /// Construct a new Number MondValue with the specified value.
        /// </summary>
        private MondValue(double value)
        {
            _type = MondValueType.Number;
            _numberValue = value;
            
            ObjectValue = null;
            ArrayValue = null;
            _stringValue = null;
            FunctionValue = null;
        }

        /// <summary>
        /// Construct a new String MondValue with the specified value.
        /// </summary>
        private MondValue(string value)
        {
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException(nameof(value));

            _type = MondValueType.String;
            _numberValue = 0;
            _stringValue = value;

            ObjectValue = null;
            ArrayValue = null;
            FunctionValue = null;
            _stringValue = value;
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value.
        /// </summary>
        private MondValue(MondFunction function)
        {
            if (ReferenceEquals(function, null))
                throw new ArgumentNullException(nameof(function));

            _type = MondValueType.Function;
            _numberValue = 0;

            ObjectValue = null;
            ArrayValue = null;
            _stringValue = null;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value. Instance functions will
        /// bind themselves to their parent object when being retrieved.
        /// </summary>
        private MondValue(MondInstanceFunction function)
        {
            if (ReferenceEquals(function, null))
                throw new ArgumentNullException(nameof(function));

            _type = MondValueType.Function;
            _numberValue = 0;

            ObjectValue = null;
            ArrayValue = null;
            _stringValue = null;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Array MondValue with the specified values.
        /// </summary>
        private MondValue(IEnumerable<MondValue> values)
            : this(MondValueType.Array)
        {
            if (ReferenceEquals(values, null))
                throw new ArgumentNullException(nameof(values));

            var arr = ArrayValue;
            foreach (var item in values)
            {
                arr.Add(item);
            }
        }

        internal MondValue(Closure closure)
        {
            _type = MondValueType.Function;
            _numberValue = 0;

            ObjectValue = null;
            ArrayValue = null;
            _stringValue = null;
            FunctionValue = closure;
        }

        /// <summary>
        /// Gets the type of this value.
        /// </summary>
        public MondValueType Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type;
        }

        /// <summary>
        /// Get or set values in the Object or Array or its' prototype.
        /// </summary>
        public MondValue this[in MondValue index]
        {
            get
            {
                if (Type == MondValueType.Array && (index.Type == MondValueType.Number || index.Type == MondValueType.Object))
                {
                    var n = (int)index;

                    if (n < 0)
                        n += ArrayValue.Count;

                    if (n < 0 || n >= ArrayValue.Count)
                        throw new MondRuntimeException(RuntimeError.IndexOutOfBounds);

                    return ArrayValue[n];
                }

                MondValue indexValue;
                if (Type == MondValueType.Object)
                {
                    if (ObjectValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapFunction(indexValue);
                }

                var i = 0;
                //ref var prototype = ref GetPrototypeReadOnly();
                var prototype = Prototype;

                while (prototype.Type == MondValueType.Object)
                {
                    var currentObjValue = prototype.ObjectValue;
                    if (currentObjValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapFunction(indexValue);

                    //prototype = ref currentValue.GetPrototypeReadOnly(); // TODO: ref reassignment
                    prototype = prototype.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type == MondValueType.Object)
                {
                    if (TryDispatch("__get", out indexValue, this, index))
                        return CheckWrapFunction(indexValue);
                }

                return Undefined;
            }
            set
            {
                if (Type == MondValueType.Array && (index.Type == MondValueType.Number || index.Type == MondValueType.Object))
                {
                    var n = (int)index;

                    if (n < 0)
                        n += ArrayValue.Count;

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

                while (prototype.Type == MondValueType.Object)
                {
                    var values = prototype.ObjectValue.Values;
                    if (values.ContainsKey(index))
                    {
                        if (prototype.ObjectValue.Locked)
                            break; // hit a wall in the prototype chain, don't continue

                        values[index] = value;
                        return;
                    }

                    prototype = prototype.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type != MondValueType.Object)
                    throw new MondRuntimeException(RuntimeError.CantCreateField, Type.GetName());

                if (ObjectValue.Locked)
                    throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

                if (TryDispatch("__set", out var _, this, index, value))
                    return;

                ObjectValue.Values[index] = value;
            }
        }

        /// <summary>
        /// Gets the dictionary instance used to store this object's values.
        /// </summary>
        public IDictionary<MondValue, MondValue> AsDictionary
        {
            get
            {
                if (Type != MondValueType.Object)
                    throw new InvalidOperationException("MondValue.AsDictionary is only valid on objects");

                return ObjectValue.Values;
            }
        }

        /// <summary>
        /// Gets the list instance used to store this array's values.
        /// </summary>
        public IList<MondValue> AsList
        {
            get
            {
                if (Type != MondValueType.Array)
                    throw new InvalidOperationException("MondValue.AsList is only valid on arrays");

                return ArrayValue;
            }
        }

        private ref MondValue GetPrototypeReadOnly()
        {
            switch (Type)
            {
                case MondValueType.Object:
                    if (ObjectValue.HasPrototype)
                        return ref ObjectValue.Prototype;

                    return ref ObjectPrototype.ValueReadOnly;

                case MondValueType.Array:
                    return ref ArrayPrototype.ValueReadOnly;

                case MondValueType.Number:
                    return ref NumberPrototype.ValueReadOnly;

                case MondValueType.String:
                    return ref StringPrototype.ValueReadOnly;

                case MondValueType.Function:
                    return ref FunctionPrototype.ValueReadOnly;

                default:
                    return ref ValuePrototype.ValueReadOnly;
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
            get => GetPrototypeReadOnly();
            set
            {
                if (Type != MondValueType.Object)
                    throw new MondRuntimeException("Prototypes can only be set on objects");

                if (ObjectValue.Locked)
                    throw new MondRuntimeException(RuntimeError.ObjectIsLocked);

                if (value.Type == MondValueType.Undefined)
                {
                    ObjectValue.HasPrototype = false;
                    ObjectValue.Prototype = Undefined;
                    return;
                }

                if (value.Type == MondValueType.Null)
                    value = ValuePrototype.Value;
                else if (value.Type != MondValueType.Object)
                    throw new MondRuntimeException("Prototypes must be an object, null, or undefined");

                ObjectValue.HasPrototype = true;
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

        public bool IsLocked => Type == MondValueType.Object && ObjectValue.Locked;

        public bool Contains(in MondValue search)
        {
            if (Type == MondValueType.String && search.Type == MondValueType.String)
                return _stringValue.Contains(search._stringValue);

            if (Type == MondValueType.Object)
            {
                if (ObjectValue.Values.ContainsKey(search))
                    return true;

                if (TryDispatch("__in", out var result, this, search))
                    return result;

                return false;
            }

            if (Type == MondValueType.Array)
                return ArrayValue.Contains(search);

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "in", Type.GetName(), search.Type.GetName());
        }

        private static T[] SliceImpl<T>(IList<T> values, MondValue? start, MondValue? end, MondValue? step)
        {
            int ToIntOrDefault(MondValue? value, int defaultValue)
            {
                if (value == null || !value.Value)
                    return defaultValue;

                return (int)value.Value;
            }

            // get start value
            var startIndex = ToIntOrDefault(start, 0);

            if (startIndex < 0)
                startIndex += values.Count;

            if (startIndex < 0 || (startIndex >= values.Count && values.Count > 0))
                throw new MondRuntimeException(RuntimeError.SliceStartBounds);

            // get end value
            var endIndex = ToIntOrDefault(end, Math.Max(0, values.Count - 1));

            if (endIndex < 0)
                endIndex += values.Count;

            if (endIndex < 0 || (endIndex >= values.Count && values.Count > 0))
                throw new MondRuntimeException(RuntimeError.SliceEndBounds);

            // get step value
            var stepValue = ToIntOrDefault(step, startIndex <= endIndex ? 1 : -1);

            if (stepValue == 0)
                throw new MondRuntimeException(RuntimeError.SliceStepZero);

            // allow reversing with default indices, ex: [::-1]
            if (stepValue < 0 && (start == null || !start.Value) && (end == null || !end.Value))
            {
                startIndex = Math.Max(0, values.Count - 1);
                endIndex = 0;
            }

            // make sure the range makes sense
            if ((stepValue < 0 && endIndex > startIndex) || (stepValue > 0 && startIndex > endIndex))
                throw new MondRuntimeException(RuntimeError.SliceInvalid); // TODO: better error message

            // find size of slice
            int length;

            if (values.Count == 0 && startIndex == 0 && endIndex == 0)
            {
                length = 0; // allow cloning empty arrays
            }
            else
            {
                var range = endIndex - startIndex + Math.Sign(stepValue);
                length = (range / stepValue) + (range % stepValue != 0 ? 1 : 0);
            }

            // copy values to new array
            var result = new T[length];

            var src = startIndex;
            for (var dst = 0; dst < length; src += stepValue, dst++)
            {
                result[dst] = values[src];
            }

            return result;
        }

        public MondValue Slice(MondValue? start = null, MondValue? end = null, MondValue? step = null)
        {
            if (Type == MondValueType.String)
                return new string(SliceImpl(_stringValue.ToCharArray(), start, end, step));

            if (Type == MondValueType.Array)
                return new MondValue(SliceImpl(ArrayValue, start, end, step));

            if (Type == MondValueType.Object)
            {
                if (TryDispatch("__slice", out var result, this, start ?? Undefined, end ?? Undefined, step ?? Undefined))
                    return result;

                throw new MondRuntimeException(RuntimeError.SliceMissingMethod);
            }

            throw new MondRuntimeException(RuntimeError.SliceWrongType, Type.GetName());
        }

        public bool Equals(in MondValue other)
        {
            switch (Type)
            {
                case MondValueType.Object:
                    if (TryDispatch("__eq", out var result, this, other))
                        return result;

                    return other.Type == MondValueType.Object && ReferenceEquals(ObjectValue, other.ObjectValue);

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

        bool IEquatable<MondValue>.Equals(MondValue other) => Equals(other);

        public int CompareTo(in MondValue other)
        {
            if (this == other)
                return 0;

            return this > other ? 1 : -1;
        }

        int IComparable<MondValue>.CompareTo(MondValue other) => CompareTo(other);

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, null))
                return false;

            var otherValue = other as MondValue?;
            if (otherValue == null)
                return false;

            return Equals(otherValue.Value);
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
                    if (TryDispatch("__hash", out var result, this))
                    {
                        if (result.Type != MondValueType.Number)
                            throw new MondRuntimeException(RuntimeError.HashWrongType);

                        return (int)result;
                    }

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
                        if (TryDispatch("__string", out var result, this))
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

        private MondValue CheckWrapFunction(in MondValue value)
        {
            if (value.Type != MondValueType.Function)
                return value;

            var funcValue = value.FunctionValue;
            if (funcValue.Type != ClosureType.InstanceNative)
                return value;

            var func = funcValue.InstanceNativeFunction;
            var inst = this;
            return new MondValue((state, args) => func(state, inst, args));
        }
        
        internal bool TryDispatch(string name, out MondValue result, params MondValue[] args)
        {
            if (Type != MondValueType.Object)
            {
                result = Undefined;
                return false;
            }
            
            // we can't use the indexer for metamethod lookups because that
            // could lead to infinite recursion, so we have a simplified
            // lookup here

            MondState state = null;
            MondValue callable;

            var current = this;
            while (true)
            {
                if (current.AsDictionary.TryGetValue(name, out callable))
                {
                    // we need to use the state from the metamethod's object
                    state = current.ObjectValue.State;

                    // and we need to wrap functions if needed
                    callable = CheckWrapFunction(callable);
                    break;
                }

                current = current.Prototype;

                if (current.Type != MondValueType.Object)
                {
                    callable = Undefined;
                    break;
                }
            }

            if (callable == Undefined)
            {
                result = Undefined;
                return false;
            }

            if (state == null)
                throw new MondRuntimeException("MondValue must have an attached state to use metamethods");

            result = state.Call(callable, args);
            return true;
        }
    }
}

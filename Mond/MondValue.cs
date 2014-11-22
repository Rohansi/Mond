using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mond.VirtualMachine;
using Mond.VirtualMachine.Prototypes;

namespace Mond
{
    [StructLayout(LayoutKind.Explicit)]
    public sealed partial class MondValue : IEquatable<MondValue>
    {
        public static readonly MondValue Undefined = new MondValue(MondValueType.Undefined);
        public static readonly MondValue Null = new MondValue(MondValueType.Null);
        public static readonly MondValue True = new MondValue(MondValueType.True);
        public static readonly MondValue False = new MondValue(MondValueType.False);

        [FieldOffset(0)]
        public readonly MondValueType Type;

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
            Type = MondValueType.String;
            _stringValue = value;
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value.
        /// </summary>
        public MondValue(MondFunction function)
        {
            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
        }

        /// <summary>
        /// Construct a new Function MondValue with the specified value. Instance functions will
        /// bind themselves to their parent object when being retrieved.
        /// </summary>
        public MondValue(MondInstanceFunction function)
        {
            Type = MondValueType.Function;
            FunctionValue = new Closure(function);
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
                        return CheckWrapInstanceNative(indexValue);
                }

                var i = 0;
                var prototype = Prototype;

                while (prototype != null)
                {
                    var currentValue = prototype;

                    if (currentValue.Type != MondValueType.Object)
                        break;

                    if (currentValue.ObjectValue.Values.TryGetValue(index, out indexValue))
                        return CheckWrapInstanceNative(indexValue);

                    prototype = currentValue.Prototype;
                    i++;

                    if (i > 100)
                        throw new MondRuntimeException(RuntimeError.CircularPrototype);
                }

                if (Type == MondValueType.Object)
                {
                    if (TryDispatch("__get", out indexValue, this, index))
                        return CheckWrapInstanceNative(indexValue);
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

                if (Type == MondValueType.Object)
                {
                    if (ObjectValue.Values.ContainsKey(index) && ObjectValue.LockState == ObjectLockState.None)
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
                    if (currentValue.ObjectValue.LockState != ObjectLockState.None)
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
                    throw new MondRuntimeException(RuntimeError.CantCreateField, Type.GetName());

                if (ObjectValue.LockState == ObjectLockState.Frozen)
                    return;

                MondValue result;
                if (TryDispatch("__set", out result, this, index, value))
                    return;

                ObjectValue.Values[index] = value;
            }
        }

        /// <summary>
        /// Prevents and object's existing fields from being removed, however their values can still be changed and new fields can still be created.
        /// </summary>
        public void Lock()
        {
            LockImpl(ObjectLockState.Locked);
        }

        /// <summary>
        /// Makes an object completely immutable. Ideal for prototypes.
        /// </summary>
        public void Freeze()
        {
            LockImpl(ObjectLockState.Frozen);
        }

        private void LockImpl(ObjectLockState lockState)
        {
            if (Type != MondValueType.Object)
                throw new MondRuntimeException("Attempt to lock non-object");

            if (ObjectValue.LockState == lockState)
                return;

            if (ObjectValue.LockState != ObjectLockState.None && lockState == ObjectLockState.None)
                throw new MondRuntimeException("Cannot unlock a locked/frozen object.");

            if (ObjectValue.LockState == ObjectLockState.Frozen)
                throw new MondRuntimeException("Cannot change the lock state of a frozen object.");

            ObjectValue.LockState = lockState;
        }

        /// <summary>
        /// Gets the prototype object for this value.
        /// 
        /// Sets the prototype object for this object. Can either be MondValueType.Object,
        /// MondValue.Null, MondValue.Undefined or null. If set to MondValue.Undefined or null,
        /// the default prototype will be used.
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

                if (value == Undefined)
                    value = null;

                if (value != null && value.Type != MondValueType.Object && value.Type != MondValueType.Null)
                    throw new MondRuntimeException("Prototypes must be an object or null");

                ObjectValue.Prototype = value;
            }
        }

        public bool Equals(MondValue other)
        {
            if (ReferenceEquals(other, null))
                return false;

            switch (Type)
            {
                case MondValueType.Object:
                    {
                        if (ReferenceEquals(ObjectValue, other.ObjectValue))
                            return true;

                        MondValue result;
                        if (TryDispatch("__eq", out result, this, other))
                            return result;

                        return false;
                    }

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

            throw new MondRuntimeException(RuntimeError.CantUseOperatorOnTypes, "in", Type, search.Type.GetName());
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

                            return result;
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

        private MondValue CheckWrapInstanceNative(MondValue value)
        {
            if (value.Type != MondValueType.Function || value.FunctionValue.Type != ClosureType.InstanceNative)
                return value;

            var func = value.FunctionValue.InstanceNativeFunction;
            var inst = this;
            return new MondValue((state, args) => func(state, inst, args));
        }

        //Convenience function on VirtualMachine.Object.TryDispatch
        private bool TryDispatch(string name, out MondValue result, params MondValue[] args)
        {
            if (Type != MondValueType.Object)
            {
                result = Undefined;
                return false;
            }

            return ObjectValue.TryDispatch(name, out result, args);
        }
    }
}

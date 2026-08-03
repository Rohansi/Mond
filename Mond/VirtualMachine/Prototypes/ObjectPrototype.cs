using System.Linq;
using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    /// <summary>
    /// Contains members available on every object.
    /// </summary>
    [MondPrototype("Object")]
    internal static partial class ObjectPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static ObjectPrototype()
        {
            ValueReadOnly = PrototypeObject.Build(ValuePrototype.Value);
        }

        private const string MustBeAnObject = "Object.{0}: must be called on an object";
        private const string LockedError = "Object.{0}: " + RuntimeError.ObjectIsLocked;

        /// <summary>
        /// Stores a value under the given key, replacing any existing entry.
        /// </summary>
        [MondFunction]
        public static void Add([MondInstance] MondValue instance, MondValue key, MondValue value)
        {
            EnsureObject("add", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "add");

            instance.ObjectValue.MayHaveMetamethods = true;
            instance.ObjectValue.Values[key] = value;
        }

        /// <summary>
        /// Removes every entry from the object.
        /// </summary>
        [MondFunction]
        public static void Clear([MondInstance] MondValue instance)
        {
            EnsureObject("clear", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "clear");

            instance.ObjectValue.Values.Clear();
        }

        /// <summary>
        /// Returns true when the object has an entry under the given key.
        /// </summary>
        [MondFunction]
        public static bool ContainsKey([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("containsKey", instance);

            return instance.ObjectValue.Values.ContainsKey(key);
        }

        /// <summary>
        /// Returns true when any entry holds a value equal to the given one.
        /// </summary>
        [MondFunction]
        public static bool ContainsValue([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("containsValue", instance);

            return instance.ObjectValue.Values.ContainsValue(value);
        }

        /// <summary>
        /// Returns the value stored under the given key, or undefined when there is none.
        /// </summary>
        [MondFunction]
        public static MondValue Get([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("get", instance);

            if (!instance.ObjectValue.Values.TryGetValue(key, out var value))
                return MondValue.Undefined;

            return value;
        }

        /// <summary>
        /// Removes the entry under the given key and returns whether one was found.
        /// </summary>
        [MondFunction]
        public static bool Remove([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("remove", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "remove");

            return instance.ObjectValue.Values.Remove(key);
        }

        /// <summary>
        /// Returns the number of entries in the object.
        /// </summary>
        [MondFunction]
        public static int Length([MondInstance] MondValue instance)
        {
            EnsureObject("length", instance);

            return instance.ObjectValue.Values.Count;
        }

        /// <summary>
        /// Returns an enumerator that yields each entry as a key and value pair.
        /// </summary>
        [MondFunction]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
            EnsureObject("getEnumerator", instance);

            var enumerator = MondValue.Object();
            var keys = instance.ObjectValue.Values.Keys.ToList();
            var i = 0;

            enumerator["current"] = MondValue.Undefined;
            enumerator["moveNext"] = MondValue.Function((_, args) =>
            {
                if (i >= keys.Count)
                    return false;

                var pair = MondValue.Object();
                pair["key"] = keys[i];
                pair["value"] = instance.ObjectValue.Values[keys[i]];

                enumerator["current"] = pair;
                i++;
                return true;
            });

            enumerator["dispose"] = new MondFunction((_, args) => MondValue.Undefined);

            return enumerator;
        }

        /// <summary>
        /// Sets the object this one inherits its members from and returns the object.
        /// </summary>
        [MondFunction]
        public static MondValue SetPrototype([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("setPrototype", instance);

            if (value.Type != MondValueType.Object && value.Type != MondValueType.Null && value.Type != MondValueType.Undefined)
                throw new MondRuntimeException("Object.setPrototype: prototype value must be an object, null, or undefined");

            instance.Prototype = value;

            return instance;
        }

        /// <summary>
        /// Makes the object read only and returns it.
        /// </summary>
        [MondFunction]
        public static MondValue Lock([MondInstance] MondValue instance)
        {
            EnsureObject("lock", instance);

            instance.Lock();
            return instance;
        }

        /// <summary>
        /// Sets the prototype, makes the object read only, and returns it.
        /// </summary>
        [MondFunction]
        public static MondValue SetPrototypeAndLock([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("setPrototypeAndLock", instance);

            SetPrototype(instance, value);
            Lock(instance);

            return instance;
        }

        private static void EnsureObject(string methodName, MondValue instance)
        {
            if (instance.Type != MondValueType.Object)
                throw new MondRuntimeException(MustBeAnObject, methodName);
        }
    }
}

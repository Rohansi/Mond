using System.Linq;
using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Object")]
    internal static class ObjectPrototype
    {
        public static readonly MondValue Value;

        static ObjectPrototype()
        {
            Value = MondPrototypeBinder.Bind(typeof(ObjectPrototype));
            Value.Prototype = ValuePrototype.Value;

            Value.Lock();
        }

        private const string MustBeAnObject = "Object.{0}: must be called on an object";
        private const string LockedError = "Object.{0}: " + RuntimeError.ObjectIsLocked;

        /// <summary>
        /// add(key, value)
        /// </summary>
        [MondFunction("add")]
        public static void Add([MondInstance] MondValue instance, MondValue key, MondValue value)
        {
            EnsureObject("add", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "add");

            instance.ObjectValue.Values[key] = value;
        }

        /// <summary>
        /// clear()
        /// </summary>
        [MondFunction("clear")]
        public static void Clear([MondInstance] MondValue instance)
        {
            EnsureObject("clear", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "clear");

            instance.ObjectValue.Values.Clear();
        }

        /// <summary>
        /// containsKey(key): bool
        /// </summary>
        [MondFunction("containsKey")]
        public static bool ContainsKey([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("containsKey", instance);

            return instance.ObjectValue.Values.ContainsKey(key);
        }

        /// <summary>
        /// containsValue(value): bool
        /// </summary>
        [MondFunction("containsValue")]
        public static bool ContainsValue([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("containsValue", instance);

            return instance.ObjectValue.Values.ContainsValue(value);
        }

        /// <summary>
        /// get(key): any
        /// </summary>
        [MondFunction("get")]
        public static MondValue Get([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("get", instance);

            if (!instance.ObjectValue.Values.TryGetValue(key, out var value))
                return MondValue.Undefined;

            return value;
        }

        /// <summary>
        /// remove(key): bool
        /// </summary>
        [MondFunction("remove")]
        public static bool Remove([MondInstance] MondValue instance, MondValue key)
        {
            EnsureObject("remove", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "remove");

            return instance.ObjectValue.Values.Remove(key);
        }

        /// <summary>
        /// length(): number
        /// </summary>
        [MondFunction("length")]
        public static int Length([MondInstance] MondValue instance)
        {
            EnsureObject("length", instance);

            return instance.ObjectValue.Values.Count;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        [MondFunction("getEnumerator")]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
            EnsureObject("getEnumerator", instance);

            var enumerator = new MondValue(MondValueType.Object);
            var keys = instance.ObjectValue.Values.Keys.ToList();
            var i = 0;

            enumerator["current"] = MondValue.Undefined;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= keys.Count)
                    return false;

                var pair = new MondValue(MondValueType.Object);
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
        /// setPrototype(value: any) : object
        /// </summary>
        [MondFunction("setPrototype")]
        public static MondValue SetPrototype([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("setPrototype", instance);

            if (value.Type != MondValueType.Object && value.Type != MondValueType.Null && value.Type != MondValueType.Undefined)
                throw new MondRuntimeException("Object.setPrototype: prototype value must be an object, null, or undefined");

            instance.Prototype = value;

            return instance;
        }

        /// <summary>
        /// lock(): object
        /// </summary>
        [MondFunction("lock")]
        public static MondValue Lock([MondInstance] MondValue instance)
        {
            EnsureObject("lock", instance);

            instance.Lock();
            return instance;
        }

        /// <summary>
        /// setPrototypeAndLock(value: any): object
        /// </summary>
        [MondFunction("setPrototypeAndLock")]
        public static MondValue SetPrototypeAndLock([MondInstance] MondValue instance, MondValue value)
        {
            EnsureObject("setPrototypeAndLock", instance);

            SetPrototype(instance, value);
            Lock(instance);

            return instance;
        }

        /// <summary>
        /// enableThis(): object
        /// </summary>
        [MondFunction("enableThis")]
        public static MondValue EnableThis([MondInstance] MondValue instance)
        {
            EnsureObject("enableThis", instance);

            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "enableThis");

            instance.EnableThis();
            return instance;
        }

        private static void EnsureObject(string methodName, MondValue instance)
        {
            if (instance.Type != MondValueType.Object)
                throw new MondRuntimeException(MustBeAnObject, methodName);
        }
    }
}

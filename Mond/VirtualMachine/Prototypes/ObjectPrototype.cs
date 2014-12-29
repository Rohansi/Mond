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

        private const string LockedError = "Object.{0}: " + RuntimeError.ObjectIsLocked;

        /// <summary>
        /// add(key, value): object
        /// </summary>
        [MondFunction("add")]
        public static MondValue Add([MondInstance] MondValue instance, MondValue key, MondValue value)
        {
            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "add");

            instance.ObjectValue.Values[key] = value;
            return instance;
        }

        /// <summary>
        /// clear(): object
        /// </summary>
        [MondFunction("clear")]
        public static MondValue Clear([MondInstance] MondValue instance)
        {
            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "clear");

            instance.ObjectValue.Values.Clear();
            return instance;
        }

        /// <summary>
        /// containsKey(key): bool
        /// </summary>
        [MondFunction("containsKey")]
        public static bool ContainsKey([MondInstance] MondValue instance, MondValue key)
        {
            return instance.ObjectValue.Values.ContainsKey(key);
        }

        /// <summary>
        /// containsValue(value): bool
        /// </summary>
        [MondFunction("containsValue")]
        public static bool ContainsValue([MondInstance] MondValue instance, MondValue value)
        {
            return instance.ObjectValue.Values.ContainsValue(value);
        }

        /// <summary>
        /// get(key): any
        /// </summary>
        [MondFunction("get")]
        public static MondValue Get([MondInstance] MondValue instance, MondValue key)
        {
            MondValue value;
            if (!instance.ObjectValue.Values.TryGetValue(key, out value))
                return MondValue.Undefined;

            return value;
        }

        /// <summary>
        /// remove(key): object
        /// </summary>
        [MondFunction("remove")]
        public static MondValue Remove([MondInstance] MondValue instance, MondValue key)
        {
            if (instance.ObjectValue.Locked)
                throw new MondRuntimeException(LockedError, "remove");

            instance.ObjectValue.Values.Remove(key);
            return instance;
        }

        /// <summary>
        /// length(): number
        /// </summary>
        [MondFunction("length")]
        public static int Length([MondInstance] MondValue instance)
        {
            return instance.ObjectValue.Values.Count;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        [MondFunction("getEnumerator")]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
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
            instance.Lock();
            return instance;
        }

        /// <summary>
        /// setPrototypeAndLock(value: any): object
        /// </summary>
        [MondFunction("setPrototypeAndLock")]
        public static MondValue SetPrototypeAndLock([MondInstance] MondValue instance, MondValue value)
        {
            SetPrototype(instance, value);
            Lock(instance);

            return instance;
        }
    }
}

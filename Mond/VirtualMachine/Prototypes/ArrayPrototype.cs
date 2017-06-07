using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Array")]
    internal static class ArrayPrototype
    {
        public static readonly MondValue Value;

        static ArrayPrototype()
        {
            Value = MondPrototypeBinder.Bind(typeof(ArrayPrototype));
            Value.Prototype = ValuePrototype.Value;

            Value.Lock();
        }

        private const string MustBeAnArray = "Array.{0}: must be called on an array";
        private const string IndexOutOfBounds = "Array.{0}: index out of bounds";

        /// <summary>
        /// add(item)
        /// </summary>
        [MondFunction("add")]
        public static void Add([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("add", instance);

            instance.ArrayValue.Add(item);
        }

        /// <summary>
        /// clear()
        /// </summary>
        [MondFunction("clear")]
        public static void Clear([MondInstance] MondValue instance)
        {
            EnsureArray("clear", instance);

            instance.ArrayValue.Clear();
        }

        /// <summary>
        /// contains(item): bool
        /// </summary>
        [MondFunction("contains")]
        public static bool Contains([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("contains", instance);

            return instance.ArrayValue.Contains(item);
        }

        /// <summary>
        /// indexOf(item): number
        /// </summary>
        [MondFunction("indexOf")]
        public static int IndexOf([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("indexOf", instance);

            return instance.ArrayValue.IndexOf(item);
        }

        /// <summary>
        /// insert(index: number, item)
        /// </summary>
        [MondFunction("insert")]
        public static void Insert([MondInstance] MondValue instance, int index, MondValue item)
        {
            EnsureArray("insert", instance);

            if (index < 0 || index > instance.ArrayValue.Count)
                throw new MondRuntimeException(IndexOutOfBounds, "insert");

            instance.ArrayValue.Insert(index, item);
        }

        /// <summary>
        /// lastIndexOf(item): number
        /// </summary>
        [MondFunction("lastIndexOf")]
        public static int LastIndexOf([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("lastIndexOf", instance);

            return instance.ArrayValue.LastIndexOf(item);
        }

        /// <summary>
        /// remove(item): bool
        /// </summary>
        [MondFunction("remove")]
        public static MondValue Remove([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("remove", instance);

            return instance.ArrayValue.Remove(item);
        }

        /// <summary>
        /// removeAt(index: number)
        /// </summary>
        [MondFunction("removeAt")]
        public static void RemoveAt([MondInstance] MondValue instance, int index)
        {
            EnsureArray("removeAt", instance);

            if (index < 0 || index >= instance.ArrayValue.Count)
                throw new MondRuntimeException(IndexOutOfBounds, "removeAt");

            instance.ArrayValue.RemoveAt(index);
        }

        /// <summary>
        /// length(): number
        /// </summary>
        [MondFunction("length")]
        public static int Length([MondInstance] MondValue instance)
        {
            EnsureArray("length", instance);

            return instance.ArrayValue.Count;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        [MondFunction("getEnumerator")]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
            EnsureArray("getEnumerator", instance);

            var enumerator = new MondValue(MondValueType.Object);
            var i = 0;

            enumerator["current"] = MondValue.Undefined;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= instance.ArrayValue.Count)
                    return false;

                enumerator["current"] = instance.ArrayValue[i++];
                return true;
            });

            enumerator["dispose"] = new MondFunction((_, args) => MondValue.Undefined);

            return enumerator;
        }

        /// <summary>
        /// slice(startIndex: number, endIndex: number, step: number): 
        /// </summary>
        [MondFunction("slice")]
        public static MondValue Slice([MondInstance] MondValue instance, int startIndex, int endIndex, int step)
        {
            EnsureArray("slice", instance);

            return instance.Slice(startIndex, endIndex, step);
        }

        private static void EnsureArray(string methodName, MondValue instance)
        {
            if (instance.Type != MondValueType.Array)
                throw new MondRuntimeException(MustBeAnArray, methodName);
        }
    }
}

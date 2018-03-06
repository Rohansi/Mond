using System.Collections.Generic;
using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("Array")]
    internal static class ArrayPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static ArrayPrototype()
        {
            ValueReadOnly = MondPrototypeBinder.Bind(typeof(ArrayPrototype));
            ValueReadOnly.Prototype = ValuePrototype.Value;

            ValueReadOnly.Lock();
        }

        private const string MustBeAnArray = "Array.{0}: must be called on an array";
        private const string IndexOutOfBounds = "Array.{0}: index out of bounds";

        /// <summary>
        /// add(item)
        /// </summary>
        [MondFunction]
        public static void Add([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("add", instance);

            instance.ArrayValue.Add(item);
        }

        /// <summary>
        /// clear()
        /// </summary>
        [MondFunction]
        public static void Clear([MondInstance] MondValue instance)
        {
            EnsureArray("clear", instance);

            instance.ArrayValue.Clear();
        }

        /// <summary>
        /// contains(item): bool
        /// </summary>
        [MondFunction]
        public static bool Contains([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("contains", instance);

            return instance.ArrayValue.Contains(item);
        }

        /// <summary>
        /// indexOf(item): number
        /// </summary>
        [MondFunction]
        public static int IndexOf([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("indexOf", instance);

            return instance.ArrayValue.IndexOf(item);
        }

        /// <summary>
        /// insert(index: number, item)
        /// </summary>
        [MondFunction]
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
        [MondFunction]
        public static int LastIndexOf([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("lastIndexOf", instance);

            return instance.ArrayValue.LastIndexOf(item);
        }

        /// <summary>
        /// remove(item): bool
        /// </summary>
        [MondFunction]
        public static MondValue Remove([MondInstance] MondValue instance, MondValue item)
        {
            EnsureArray("remove", instance);

            return instance.ArrayValue.Remove(item);
        }

        /// <summary>
        /// removeAt(index: number)
        /// </summary>
        [MondFunction]
        public static void RemoveAt([MondInstance] MondValue instance, int index)
        {
            EnsureArray("removeAt", instance);

            if (index < 0 || index >= instance.ArrayValue.Count)
                throw new MondRuntimeException(IndexOutOfBounds, "removeAt");

            instance.ArrayValue.RemoveAt(index);
        }

        /// <summary>
        /// sort()
        /// </summary>
        [MondFunction]
        public static void Sort([MondInstance] MondValue instance) =>
            SortImpl("sort", instance, 0, instance.ArrayValue.Count, false);

        /// <summary>
        /// sort(index: number, count: number)
        /// </summary>
        [MondFunction]
        public static void Sort([MondInstance] MondValue instance, int index, int count) =>
            SortImpl("sort", instance, index, count, false);

        /// <summary>
        /// sortDescending()
        /// </summary>
        [MondFunction]
        public static void SortDescending([MondInstance] MondValue instance) =>
            SortImpl("sortDescending", instance, 0, instance.ArrayValue.Count, true);

        /// <summary>
        /// sortDescending(index: number, count: number)
        /// </summary>
        [MondFunction]
        public static void SortDescending([MondInstance] MondValue instance, int index, int count) =>
            SortImpl("sortDescending", instance, index, count, true);

        private static void SortImpl(string name, MondValue instance, int index, int count, bool reverse)
        {
            EnsureArray(name, instance);

            if (index < 0 || index >= instance.ArrayValue.Count ||
                count < 0 || index + count > instance.ArrayValue.Count)
            {
                throw new MondRuntimeException(IndexOutOfBounds, name);
            }

            var comparer = reverse ? ReverseComparer<MondValue>.Instance : Comparer<MondValue>.Default;
            instance.ArrayValue.Sort(index, count, comparer);
        }

        /// <summary>
        /// length(): number
        /// </summary>
        [MondFunction]
        public static int Length([MondInstance] MondValue instance)
        {
            EnsureArray("length", instance);

            return instance.ArrayValue.Count;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        [MondFunction]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
            EnsureArray("getEnumerator", instance);

            var enumerator = MondValue.Object();
            var i = 0;

            enumerator["current"] = MondValue.Undefined;
            enumerator["moveNext"] = MondValue.Function((_, args) =>
            {
                if (i >= instance.ArrayValue.Count)
                    return false;

                enumerator["current"] = instance.ArrayValue[i++];
                return true;
            });

            enumerator["dispose"] = new MondFunction((_, args) => MondValue.Undefined);

            return enumerator;
        }

        private static void EnsureArray(string methodName, MondValue instance)
        {
            if (instance.Type != MondValueType.Array)
                throw new MondRuntimeException(MustBeAnArray, methodName);
        }
    }
}

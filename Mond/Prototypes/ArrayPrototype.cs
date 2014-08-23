using System.Collections.Generic;

namespace Mond.Prototypes
{
    static class ArrayPrototype
    {
        public static readonly MondValue Value;

        static ArrayPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value.Prototype = ValuePrototype.Value;

            Value["add"] = new MondInstanceFunction(Add);
            Value["clear"] = new MondInstanceFunction(Clear);
            Value["contains"] = new MondInstanceFunction(Contains);
            Value["indexOf"] = new MondInstanceFunction(IndexOf);
            Value["lastIndexOf"] = new MondInstanceFunction(LastIndexOf);
            Value["insert"] = new MondInstanceFunction(Insert);
            Value["remove"] = new MondInstanceFunction(Remove);
            Value["removeAt"] = new MondInstanceFunction(RemoveAt);

            Value["length"] = new MondInstanceFunction(Length);
            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);

            Value.Lock();
        }

        private const string IndexOutOfBounds = "Array.{0}: index out of bounds";

        /// <summary>
        /// add(item): array
        /// </summary>
        private static MondValue Add(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("add", instance.Type, arguments, MondValueType.Undefined);

            instance.ArrayValue.Add(arguments[0]);
            return instance;
        }

        /// <summary>
        /// clear(): array
        /// </summary>
        private static MondValue Clear(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("clear", instance.Type, arguments);

            instance.ArrayValue.Clear();
            return instance;
        }

        /// <summary>
        /// contains(item): bool
        /// </summary>
        private static MondValue Contains(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("contains", instance.Type, arguments, MondValueType.Undefined);
            return instance.ArrayValue.Contains(arguments[0]);
        }

        /// <summary>
        /// indexOf(item): number
        /// </summary>
        private static MondValue IndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("indexOf", instance.Type, arguments, MondValueType.Undefined);
            return instance.ArrayValue.IndexOf(arguments[0]);
        }

        /// <summary>
        /// insert(index: number, item): array
        /// </summary>
        private static MondValue Insert(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("insert", instance.Type, arguments, MondValueType.Number, MondValueType.Undefined);

            var index = (int)arguments[0];

            if (index < 0 || index > instance.ArrayValue.Count)
                throw new MondRuntimeException(IndexOutOfBounds, "insert");

            instance.ArrayValue.Insert(index, arguments[1]);
            return instance;
        }

        /// <summary>
        /// lastIndexOf(item): number
        /// </summary>
        private static MondValue LastIndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("lastIndexOf", instance.Type, arguments, MondValueType.Undefined);
            return instance.ArrayValue.LastIndexOf(arguments[0]);
        }

        /// <summary>
        /// remove(item): array
        /// </summary>
        private static MondValue Remove(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("remove", instance.Type, arguments, MondValueType.Undefined);
            instance.ArrayValue.Remove(arguments[0]);
            return instance;
        }

        /// <summary>
        /// removeAt(index: number): array
        /// </summary>
        private static MondValue RemoveAt(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("removeAt", instance.Type, arguments, MondValueType.Number);

            var index = (int)arguments[0];

            if (index < 0 || index >= instance.ArrayValue.Count)
                throw new MondRuntimeException(IndexOutOfBounds, "removeAt");

            instance.ArrayValue.RemoveAt((int)arguments[0]);
            return instance;
        }

        /// <summary>
        /// length(): number
        /// </summary>
        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments);
            return instance.ArrayValue.Count;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        private static MondValue GetEnumerator(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("getEnumerator", instance.Type, arguments);

            var enumerator = new MondValue(MondValueType.Object);
            var i = 0;

            enumerator["current"] = MondValue.Null;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= instance.ArrayValue.Count)
                    return false;

                enumerator["current"] = instance.ArrayValue[i++];
                return true;
            });

            return enumerator;
        }

        private static void Check(string method, MondValueType type, IList<MondValue> arguments, params MondValueType[] requiredTypes)
        {
            if (type != MondValueType.Array)
                throw new MondRuntimeException("Array.{0}: must be called on an Array", method);

            if (arguments.Count < requiredTypes.Length)
                throw new MondRuntimeException("Array.{0}: must be called with {1} argument{2}", method, requiredTypes.Length, requiredTypes.Length == 1 ? "" : "s");

            for (var i = 0; i < requiredTypes.Length; i++)
            {
                if (requiredTypes[i] == MondValueType.Undefined)
                    continue;

                if (arguments[i].Type != requiredTypes[i])
                    throw new MondRuntimeException("Array.{0}: argument {1} must be of type {2}", method, i + 1, requiredTypes[i]);
            }
        }
    }
}

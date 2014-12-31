using System.Collections.Generic;

namespace Mond
{
    public sealed partial class MondValue
    {
        public bool IsEnumerable
        {
            get
            {
                var hasGetEnumerator = this["getEnumerator"].Type == MondValueType.Function;
                var hasEnumeratorFunc = this["moveNext"].Type == MondValueType.Function;

                return hasGetEnumerator || hasEnumeratorFunc;
            }
        }

        public IEnumerable<MondValue> Enumerate(MondState state)
        {
            var enumerator = this;
            var moveNext = enumerator["moveNext"];

            if (moveNext.Type != MondValueType.Function)
            {
                var getEnumerator = this["getEnumerator"];
                if (getEnumerator.Type != MondValueType.Function)
                    throw new MondRuntimeException("Value is not enumerable");

                enumerator = state.Call(getEnumerator);

                moveNext = enumerator["moveNext"];
                if (moveNext.Type != MondValueType.Function)
                    throw new MondRuntimeException("Value is not enumerable");
            }

            while (state.Call(moveNext))
            {
                yield return enumerator["current"];
            }
        }

        public static MondValue FromEnumerable(IEnumerable<MondValue> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            var enumerableObj = new MondValue(MondValueType.Object);

            enumerableObj["current"] = Null;

            enumerableObj["moveNext"] = new MondFunction((_, args) =>
            {
                var success = enumerator.MoveNext();
                enumerableObj["current"] = success ? enumerator.Current : Null;
                return success;
            });

            enumerableObj["dispose"] = new MondFunction((_, args) =>
            {
                enumerator.Dispose();
                return Undefined;
            });

            enumerableObj["getEnumerator"] = new MondFunction((_, args) => enumerableObj);

            return enumerableObj;
        }

        /// <summary>
        /// Enumerates an Object or Array without going through the Mond VM.
        /// </summary>
        public IEnumerable<MondValue> RawEnumerate()
        {
            switch (Type)
            {
                case MondValueType.Array:
                    foreach (var e in ArrayValue)
                    {
                        yield return e;
                    }

                    break;

                case MondValueType.Object:
                    foreach (var key in ObjectValue.Values.Keys)
                    {
                        var value = this[key];

                        var pair = new MondValue(MondValueType.Object);
                        pair["key"] = key;
                        pair["value"] = value;

                        yield return pair;
                    }

                    break;

                default:
                    throw new MondRuntimeException("RawEnumerate can only be used on Object or Array");
            }
        }
    }
}

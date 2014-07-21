using System.Collections.Generic;

namespace Mond
{
    public partial class MondValue
    {
        public bool IsEnumerable
        {
            get
            {
                var hasGetEnumerator = this["getEnumerator"].Type == MondValueType.Closure;
                var hasEnumeratorFunc = this["moveNext"].Type == MondValueType.Closure;

                return hasGetEnumerator || hasEnumeratorFunc;
            }
        }

        public IEnumerable<MondValue> Enumerate(MondState state)
        {
            var enumerator = this;
            var moveNext = enumerator["moveNext"];

            if (moveNext.Type != MondValueType.Closure)
            {
                var getEnumerator = this["getEnumerator"];
                if (getEnumerator.Type != MondValueType.Closure)
                    throw new MondRuntimeException("Value is not enumerable");

                enumerator = state.Call(getEnumerator);

                moveNext = enumerator["moveNext"];
                if (moveNext.Type != MondValueType.Closure)
                    throw new MondRuntimeException("Value is not enumerable");
            }

            while (state.Call(moveNext))
            {
                yield return enumerator["current"];
            }
        }
    }
}

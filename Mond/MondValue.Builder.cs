using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mond
{
    public partial struct MondValue
    {
        public static MondValue Number(double value)
        {
            return new MondValue(value);
        }

        public static MondValue String([NotNull] string value)
        {
            return new MondValue(value);
        }

        public static MondValue Function([NotNull] MondFunction value)
        {
            return new MondValue(value);
        }

        public static MondValue Function([NotNull] MondInstanceFunction value)
        {
            return new MondValue(value);
        }

        public static MondValue Object([NotNull] MondState state, IEnumerable<KeyValuePair<MondValue, MondValue>> entries = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var value = new MondValue(state);

            if (entries != null)
            {
                var dict = value.AsDictionary;
                foreach (var kvp in entries)
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }

            return value;
        }

        public static MondValue Object(IEnumerable<KeyValuePair<MondValue, MondValue>> entries = null)
        {
            var value = new MondValue(MondValueType.Object);

            if (entries != null)
            {
                var dict = value.AsDictionary;
                foreach (var kvp in entries)
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }

            return value;
        }

        public static MondValue Array(IEnumerable<MondValue> values = null)
        {
            if (values == null)
            {
                return new MondValue(MondValueType.Array);
            }

            return new MondValue(values);
        }
    }
}

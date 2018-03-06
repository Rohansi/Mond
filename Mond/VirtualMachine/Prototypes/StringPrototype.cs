using System;
using System.Linq;
using Mond.Binding;

namespace Mond.VirtualMachine.Prototypes
{
    [MondModule("String")]
    internal static class StringPrototype
    {
        internal static MondValue ValueReadOnly;
        public static MondValue Value => ValueReadOnly;

        static StringPrototype()
        {
            ValueReadOnly = MondPrototypeBinder.Bind(typeof(StringPrototype));
            ValueReadOnly.Prototype = ValuePrototype.Value;

            ValueReadOnly.Lock();
        }

        private const string IndexOutOfBounds = "String.{0}: index out of bounds";

        /// <summary>
        /// charAt(index: number): string
        /// </summary>
        [MondFunction]
        public static string CharAt([MondInstance] MondValue instance, int index)
        {
            var instStr = (string)instance;

            if (index < 0 || index >= instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "charAt");

            return new string(instStr[index], 1);
        }

        /// <summary>
        /// charCodeAt(index: number): number
        /// </summary>
        [MondFunction]
        public static int CharCodeAt([MondInstance] MondValue instance, int index)
        {
            var instStr = (string)instance;

            if (index < 0 || index >= instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "charCodeAt");

            return instStr[index];
        }

        /// <summary>
        /// contains(value: string): bool
        /// </summary>
        [MondFunction]
        public static bool Contains([MondInstance] MondValue instance, string value)
        {
            return ((string)instance).Contains(value);
        }

        /// <summary>
        /// endsWith(value: string): bool
        /// </summary>
        [MondFunction]
        public static bool EndsWith([MondInstance] MondValue instance, string value)
        {
            return ((string)instance).EndsWith(value);
        }

        /// <summary>
        /// indexOf(value: string): number
        /// </summary>
        [MondFunction]
        public static int IndexOf([MondInstance] MondValue instance, string value)
        {
            return ((string)instance).IndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// insert(index: number, value: string): string
        /// </summary>
        [MondFunction]
        public static string Insert([MondInstance] MondValue instance, int index, string value)
        {
            var instStr = (string)instance;

            if (index < 0 || index > instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "insert");

            return instStr.Insert(index, value);
        }

        /// <summary>
        /// lastIndexOf(value: string): number
        /// </summary>
        [MondFunction]
        public static int LastIndexOf([MondInstance] MondValue instance, string value)
        {
            return ((string)instance).LastIndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// replace(oldValue: string, newValue: string): string
        /// </summary>
        [MondFunction]
        public static string Replace([MondInstance] MondValue instance, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue))
                return instance;

            return ((string)instance).Replace(oldValue, newValue);
        }

        /// <summary>
        /// split(separator: string): array
        /// </summary>
        [MondFunction]
        public static MondValue Split([MondInstance] MondValue instance, string separator)
        {
            var values = ((string)instance).Split(new [] { separator }, StringSplitOptions.None);
            return MondValue.Array(values.Select(v => (MondValue)v));
        }

        /// <summary>
        /// startsWith(value: string): bool
        /// </summary>
        [MondFunction]
        public static bool StartsWith([MondInstance] MondValue instance, string value)
        {
            return ((string)instance).StartsWith(value);
        }

        /// <summary>
        /// substring(startIndex: number): string
        /// </summary>
        [MondFunction]
        public static string Substring([MondInstance] MondValue instance, int startIndex)
        {
            var instStr = (string)instance;

            if (startIndex < 0 || startIndex >= instStr.Length)
                return "";

            return instStr.Substring(startIndex);
        }

        /// <summary>
        /// substring(startIndex: number, length: number): string
        /// </summary>
        [MondFunction]
        public static string Substring([MondInstance] MondValue instance, int startIndex, int length)
        {
            var instStr = (string)instance;

            if (startIndex < 0 || startIndex >= instStr.Length)
                return "";

            if (startIndex + length >= instStr.Length)
                length = Math.Max(instStr.Length - startIndex, 0);

            return instStr.Substring(startIndex, length);
        }

        /// <summary>
        /// toUpper(): string
        /// </summary>
        [MondFunction]
        public static string ToUpper([MondInstance] MondValue instance)
        {
            return ((string)instance).ToUpper();
        }

        /// <summary>
        /// toLower(): string
        /// </summary>
        [MondFunction]
        public static string ToLower([MondInstance] MondValue instance)
        {
            return ((string)instance).ToLower();
        }

        /// <summary>
        /// trim(): string
        /// </summary>
        [MondFunction]
        public static string Trim([MondInstance] MondValue instance)
        {
            return ((string)instance).Trim();
        }

        /// <summary>
        /// format(): string
        /// </summary>
        [MondFunction]
        public static string Format([MondInstance] MondValue instance, params MondValue[] arguments)
        {
            var values = arguments.Select<MondValue, object>(x =>
            {
                // System.String.Format has certain format specifiers
                // that are valid for integers but not floats
                // (ex. String.Format( "{0:x2}", 1.23f ); throws FormatException
                // So we treat all whole numbers as integers, everything else
                // remains unchanged.
                if (x.Type == MondValueType.Number)
                {
                    if (x % 1.0 == 0.0)
                        return (int)x;

                    return (double)x;
                }

                return x.ToString();
            }).ToArray();

            return string.Format(instance.ToString(), values);
        }

        /// <summary>
        /// length(): number
        /// </summary>
        [MondFunction]
        public static int Length([MondInstance] MondValue instance)
        {
            return ((string)instance).Length;
        }

        /// <summary>
        /// getEnumerator(): object
        /// </summary>
        [MondFunction]
        public static MondValue GetEnumerator([MondInstance] MondValue instance)
        {
            var enumerator = MondValue.Object();
            var str = (string)instance;
            var i = 0;

            enumerator["current"] = MondValue.Undefined;
            enumerator["moveNext"] = MondValue.Function((_, args) =>
            {
                if (i >= str.Length)
                    return false;

                enumerator["current"] = new string(str[i++], 1);
                return true;
            });

            enumerator["dispose"] = new MondFunction((_, args) => MondValue.Undefined);

            return enumerator;
        }
    }
}

using System;
using System.Globalization;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Char")]
    internal class CharModule
    {
        [MondFunction]
        public static short ToNumber(string s, int index = 0)
        {
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (short)s[index];
        }

        [MondFunction]
        public static string FromNumber(short num) => "" + (char)num;

        [MondFunction]
        public static string ConvertFromUtf32(int utf32) => char.ConvertFromUtf32(utf32);

        [MondFunction]
        public static int ConvertToUtf32(string s, int index = 0) => char.ConvertToUtf32(s, index);

        [MondFunction]
        public static double GetNumericValue(string s, int index = 0) => char.GetNumericValue(s, index);

        [MondFunction]
        public static string GetUnicodeCategory(string s, int index = 0) => CharUnicodeInfo.GetUnicodeCategory(s, index).ToString();

        [MondFunction]
        public static bool IsControl(string s, int index = 0) => char.IsControl(s, index);

        [MondFunction]
        public static bool IsDigit(string s, int index = 0) => char.IsDigit(s, index);

        [MondFunction]
        public static bool IsHighSurrogate(string s, int index = 0) => char.IsHighSurrogate(s, index);

        [MondFunction]
        public static bool IsLetter(string s, int index = 0) => char.IsLetter(s, index);

        [MondFunction]
        public static bool IsLetterOrDigit(string s, int index = 0) => char.IsLetterOrDigit(s, index);

        [MondFunction]
        public static bool IsLower(string s, int index = 0) => char.IsLower(s, index);

        [MondFunction]
        public static bool IsLowSurrogate(string s, int index = 0) => char.IsLowSurrogate(s, index);

        [MondFunction]
        public static bool IsNumber(string s, int index = 0) => char.IsNumber(s, index);

        [MondFunction]
        public static bool IsPunctuation(string s, int index = 0) => char.IsPunctuation(s, index);

        [MondFunction]
        public static bool IsSeparator(string s, int index = 0) => char.IsSeparator(s, index);

        [MondFunction]
        public static bool IsSurrogate(string s, int index = 0) => char.IsSurrogate(s, index);

        [MondFunction]
        public static bool IsSurrogatePair(string s, int index = 0) => char.IsSurrogatePair(s, index);

        [MondFunction]
        public static bool IsSymbol(string s, int index = 0) => char.IsSymbol(s, index);

        [MondFunction]
        public static bool IsUpper(string s, int index = 0) => char.IsUpper(s, index);

        [MondFunction]
        public static bool IsWhiteSpace(string s, int index = 0) => char.IsWhiteSpace(s, index);
    }
}

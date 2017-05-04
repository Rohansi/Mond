using System;
using System.Globalization;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Char")]
    internal class CharModule
    {
        [MondFunction("toNumber")]
        public static short ToNumber(string s, int index = 0)
        {
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (short)s[index];
        }

        [MondFunction("fromNumber")]
        public static string FromNumber(short num) => "" + (char)num;

        [MondFunction("convertFromUtf32")]
        public static string ConvertFromUtf32(int utf32) => char.ConvertFromUtf32(utf32);

        [MondFunction("convertToUtf32")]
        public static int ConvertToUtf32(string s, int index = 0) => char.ConvertToUtf32(s, index);

        [MondFunction("getNumericValue")]
        public static double GetNumericValue(string s, int index = 0) => char.GetNumericValue(s, index);

        [MondFunction("getUnicodeCategory")]
        public static string GetUnicodeCategory(string s, int index = 0) => CharUnicodeInfo.GetUnicodeCategory(s, index).ToString();

        [MondFunction("isControl")]
        public static bool IsControl(string s, int index = 0) => char.IsControl(s, index);

        [MondFunction("isDigit")]
        public static bool IsDigit(string s, int index = 0) => char.IsDigit(s, index);

        [MondFunction("isHighSurrogate")]
        public static bool IsHighSurrogate(string s, int index = 0) => char.IsHighSurrogate(s, index);

        [MondFunction("isLetter")]
        public static bool IsLetter(string s, int index = 0) => char.IsLetter(s, index);

        [MondFunction("isLetterOrDigit")]
        public static bool IsLetterOrDigit(string s, int index = 0) => char.IsLetterOrDigit(s, index);

        [MondFunction("isLower")]
        public static bool IsLower(string s, int index = 0) => char.IsLower(s, index);

        [MondFunction("isLowSurrogate")]
        public static bool IsLowSurrogate(string s, int index = 0) => char.IsLowSurrogate(s, index);

        [MondFunction("isNumber")]
        public static bool IsNumber(string s, int index = 0) => char.IsNumber(s, index);

        [MondFunction("isPunctuation")]
        public static bool IsPunctuation(string s, int index = 0) => char.IsPunctuation(s, index);

        [MondFunction("isSeparator")]
        public static bool IsSeparator(string s, int index = 0) => char.IsSeparator(s, index);

        [MondFunction("isSurrogate")]
        public static bool IsSurrogate(string s, int index = 0) => char.IsSurrogate(s, index);

        [MondFunction("isSurrogatePair")]
        public static bool IsSurrogatePair(string s, int index = 0) => char.IsSurrogatePair(s, index);

        [MondFunction("isSymbol")]
        public static bool IsSymbol(string s, int index = 0) => char.IsSymbol(s, index);

        [MondFunction("isUpper")]
        public static bool IsUpper(string s, int index = 0) => char.IsUpper(s, index);

        [MondFunction("isWhiteSpace")]
        public static bool IsWhiteSpace(string s, int index = 0) => char.IsWhiteSpace(s, index);
    }
}

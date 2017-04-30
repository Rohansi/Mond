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
                throw new ArgumentOutOfRangeException("index");

            return (short)s[index];
        }

        [MondFunction("fromNumber")]
        public static string FromNumber(short num)
        {
            return "" + (char)num;
        }

        [MondFunction("convertFromUtf32")]
        public static string ConvertFromUtf32(int utf32)
        {
            return char.ConvertFromUtf32(utf32);
        }

        [MondFunction("convertToUtf32")]
        public static int ConvertToUtf32(string s, int index = 0)
        {
            return char.ConvertToUtf32(s, index);
        }

        [MondFunction("getNumericValue")]
        public static double GetNumericValue(string s, int index = 0)
        {
            return char.GetNumericValue(s, index);
        }

        [MondFunction("getUnicodeCategory")]
        public static string GetUnicodeCategory(string s, int index = 0)
        {
            return CharUnicodeInfo.GetUnicodeCategory(s, index).ToString();
        }

        [MondFunction("isControl")]
        public static bool IsControl(string s, int index = 0)
        {
            return char.IsControl(s, index);
        }

        [MondFunction("isDigit")]
        public static bool IsDigit(string s, int index = 0)
        {
            return char.IsDigit(s, index);
        }

        [MondFunction("isHighSurrogate")]
        public static bool IsHighSurrogate(string s, int index = 0)
        {
            return char.IsHighSurrogate(s, index);
        }

        [MondFunction("isLetter")]
        public static bool IsLetter(string s, int index = 0)
        {
            return char.IsLetter(s, index);
        }

        [MondFunction("isLetterOrDigit")]
        public static bool IsLetterOrDigit(string s, int index = 0)
        {
            return char.IsLetterOrDigit(s, index);
        }

        [MondFunction("isLower")]
        public static bool IsLower(string s, int index = 0)
        {
            return char.IsLower(s, index);
        }

        [MondFunction("isLowSurrogate")]
        public static bool IsLowSurrogate(string s, int index = 0)
        {
            return char.IsLowSurrogate(s, index);
        }

        [MondFunction("isNumber")]
        public static bool IsNumber(string s, int index = 0)
        {
            return char.IsNumber(s, index);
        }

        [MondFunction("isPunctuation")]
        public static bool IsPunctuation(string s, int index = 0)
        {
            return char.IsPunctuation(s, index);
        }

        [MondFunction("isSeparator")]
        public static bool IsSeparator(string s, int index = 0)
        {
            return char.IsSeparator(s, index);
        }

        [MondFunction("isSurrogate")]
        public static bool IsSurrogate(string s, int index = 0)
        {
            return char.IsSurrogate(s, index);
        }

        [MondFunction("isSurrogatePair")]
        public static bool IsSurrogatePair(string s, int index = 0)
        {
            return char.IsSurrogatePair(s, index);
        }

        [MondFunction("isSymbol")]
        public static bool IsSymbol(string s, int index = 0)
        {
            return char.IsSymbol(s, index);
        }

        [MondFunction("isUpper")]
        public static bool IsUpper(string s, int index = 0)
        {
            return char.IsUpper(s, index);
        }

        [MondFunction("isWhiteSpace")]
        public static bool IsWhiteSpace(string s, int index = 0)
        {
            return char.IsWhiteSpace(s, index);
        }
    }
}

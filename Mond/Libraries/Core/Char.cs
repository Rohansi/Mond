using System;
using System.Globalization;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    /// <summary>
    /// Inspects and converts individual characters of a string.
    /// </summary>
    [MondModule("Char")]
    internal static partial class CharModule
    {
        /// <summary>
        /// Returns the character code of the character at the given index.
        /// </summary>
        [MondFunction]
        public static short ToNumber(string s, int index = 0)
        {
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (short)s[index];
        }

        /// <summary>
        /// Returns the single character string for the given character code.
        /// </summary>
        [MondFunction]
        public static string FromNumber(short num) => "" + (char)num;

        /// <summary>
        /// Returns the string for a Unicode code point, which may be a surrogate pair.
        /// </summary>
        [MondFunction]
        public static string ConvertFromUtf32(int utf32) => char.ConvertFromUtf32(utf32);

        /// <summary>
        /// Returns the Unicode code point at the given index, combining surrogate pairs.
        /// </summary>
        [MondFunction]
        public static int ConvertToUtf32(string s, int index = 0) => char.ConvertToUtf32(s, index);

        /// <summary>
        /// Returns the numeric value the character represents, or -1 when it has none.
        /// </summary>
        [MondFunction]
        public static double GetNumericValue(string s, int index = 0) => char.GetNumericValue(s, index);

        /// <summary>
        /// Returns the name of the Unicode category the character belongs to.
        /// </summary>
        [MondFunction]
        public static string GetUnicodeCategory(string s, int index = 0) => CharUnicodeInfo.GetUnicodeCategory(s, index).ToString();

        /// <summary>
        /// Returns true when the character is a control character.
        /// </summary>
        [MondFunction]
        public static bool IsControl(string s, int index = 0) => char.IsControl(s, index);

        /// <summary>
        /// Returns true when the character is a decimal digit.
        /// </summary>
        [MondFunction]
        public static bool IsDigit(string s, int index = 0) => char.IsDigit(s, index);

        /// <summary>
        /// Returns true when the character is the first half of a surrogate pair.
        /// </summary>
        [MondFunction]
        public static bool IsHighSurrogate(string s, int index = 0) => char.IsHighSurrogate(s, index);

        /// <summary>
        /// Returns true when the character is a letter.
        /// </summary>
        [MondFunction]
        public static bool IsLetter(string s, int index = 0) => char.IsLetter(s, index);

        /// <summary>
        /// Returns true when the character is a letter or a decimal digit.
        /// </summary>
        [MondFunction]
        public static bool IsLetterOrDigit(string s, int index = 0) => char.IsLetterOrDigit(s, index);

        /// <summary>
        /// Returns true when the character is a lower case letter.
        /// </summary>
        [MondFunction]
        public static bool IsLower(string s, int index = 0) => char.IsLower(s, index);

        /// <summary>
        /// Returns true when the character is the second half of a surrogate pair.
        /// </summary>
        [MondFunction]
        public static bool IsLowSurrogate(string s, int index = 0) => char.IsLowSurrogate(s, index);

        /// <summary>
        /// Returns true when the character is any kind of number, including fractions and subscripts.
        /// </summary>
        [MondFunction]
        public static bool IsNumber(string s, int index = 0) => char.IsNumber(s, index);

        /// <summary>
        /// Returns true when the character is punctuation.
        /// </summary>
        [MondFunction]
        public static bool IsPunctuation(string s, int index = 0) => char.IsPunctuation(s, index);

        /// <summary>
        /// Returns true when the character is a space, line, or paragraph separator.
        /// </summary>
        [MondFunction]
        public static bool IsSeparator(string s, int index = 0) => char.IsSeparator(s, index);

        /// <summary>
        /// Returns true when the character is either half of a surrogate pair.
        /// </summary>
        [MondFunction]
        public static bool IsSurrogate(string s, int index = 0) => char.IsSurrogate(s, index);

        /// <summary>
        /// Returns true when the character and the one after it form a surrogate pair.
        /// </summary>
        [MondFunction]
        public static bool IsSurrogatePair(string s, int index = 0) => char.IsSurrogatePair(s, index);

        /// <summary>
        /// Returns true when the character is a symbol, such as a currency or math sign.
        /// </summary>
        [MondFunction]
        public static bool IsSymbol(string s, int index = 0) => char.IsSymbol(s, index);

        /// <summary>
        /// Returns true when the character is an upper case letter.
        /// </summary>
        [MondFunction]
        public static bool IsUpper(string s, int index = 0) => char.IsUpper(s, index);

        /// <summary>
        /// Returns true when the character is whitespace.
        /// </summary>
        [MondFunction]
        public static bool IsWhiteSpace(string s, int index = 0) => char.IsWhiteSpace(s, index);
    }
}

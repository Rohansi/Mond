using System;
using System.Globalization;
using Mond.Compiler.Expressions;

namespace Mond.Compiler.Parselets
{
    enum NumberFormat
    {
        Decimal, Hexadecimal, Binary
    }

    class NumberParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var format = (NumberFormat)token.Tag;

            bool success;
            double value;
            int intValue;

            switch (format)
            {
                case NumberFormat.Hexadecimal:
                    success = TryHexToInt32(token.Contents, out intValue);
                    value = intValue;
                    break;

                case NumberFormat.Binary:
                    success = TryBinToInt32(token.Contents, out intValue);
                    value = intValue;
                    break;

                case NumberFormat.Decimal:
                    success = double.TryParse(token.Contents, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value);
                    break;

                default:
                    throw new NotSupportedException("Unimplemented number format: " + format.GetName());
            }

            if (!success)
                throw new MondCompilerException(token.FileName, token.Line, token.Column, CompilerError.InvalidNumber, format.GetName(), token.Contents);

            return new NumberExpression(token, value);
        }

        private static bool TryHexToInt32(string number, out int result)
        {
            result = 0;

            if (string.IsNullOrEmpty(number) || number.Length > 8) // max 8 chars per number
                return false;

            foreach (var c in number)
            {
                result <<= 4; // 4 bits per digit

                int add;
                if (c >= '0' && c <= '9')
                    add = c - '0';
                else if (c >= 'A' && c <= 'F')
                    add = 10 + (c - 'A');
                else if (c >= 'a' && c <= 'f')
                    add = 10 + (c - 'a');
                else
                    return false;

                result |= add;
            }

            return true;
        }

        private static bool TryBinToInt32(string number, out int result)
        {
            result = 0;

            if (string.IsNullOrEmpty(number) || number.Length > 32) // max 32 chars per number
                return false;

            foreach (var c in number)
            {
                result <<= 1; // 1 bit per digit

                int add;
                if (c >= '0' && c <= '1')
                    add = c - '0';
                else
                    return false;

                result |= add;
            }

            return true;
        }
    }

    static class NumberFormatExtensions
    {
        public static string GetName(this NumberFormat format)
        {
            return format.ToString().ToLower();
        }
    }
}

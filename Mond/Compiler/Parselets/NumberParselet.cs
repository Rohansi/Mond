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
            var value = 0D;
            var format = (NumberFormat)token.Tag;
            var end = -1;

            switch (format)
            {
                case NumberFormat.Hexadecimal:
                    end = token.Contents.Length - 1;
                    for (var i = end; i >= 0; --i)
                    {
                        byte hex;

                        if (!byte.TryParse(token.Contents[i].ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out hex))
                            throw new MondCompilerException(token.FileName, token.Line, token.Column, CompilerError.InvalidNumber, format.GetName(), token.Contents);

                        value += hex * Math.Pow(16, end - i);
                    }

                    break;

                case NumberFormat.Binary:
                    end = token.Contents.Length - 1;
                    var accumulator = 1;
                    for (var i = end; i >= 0; --i)
                    {
                        var ch = token.Contents[i];

                        if (ch != '0' && ch != '1')
                            throw new MondCompilerException(token.FileName, token.Line, token.Column, CompilerError.InvalidNumber, format.GetName(), token.Contents);

                        if (ch == '1')
                            value += accumulator;

                        accumulator *= 2;
                    }

                    break;

                case NumberFormat.Decimal:
                    if(!double.TryParse(token.Contents, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value))
                        throw new MondCompilerException(token.FileName, token.Line, token.Column, CompilerError.InvalidNumber, format.GetName(), token.Contents);

                    break;

                default:
                    throw new NotImplementedException(String.Format("Unimplemented number format '{0}'.", format.GetName()));
            }

            return new NumberExpression(token, value);
        }
    }

    static class NumberFormatExtensions
    {
        public static string GetName(this NumberFormat format)
        {
            return Enum.GetName(typeof(NumberFormat), format).ToLower();
        }
    }
}

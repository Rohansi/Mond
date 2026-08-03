using System.Globalization;
using Mond.Binding;

namespace Mond.Libraries.Core;

[MondModule("Parse", bareMethods: true)]
internal static partial class ParseModule
{
    /// <summary>
    /// Parses the string as a decimal number, returning undefined when it is not one.
    /// </summary>
    [MondFunction]
    public static MondValue ParseFloat(string str)
    {
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }

    /// <summary>
    /// Parses the string as a whole number, returning undefined when it is not one.
    /// </summary>
    [MondFunction]
    public static MondValue ParseInt(string str)
    {
        return long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }

    /// <summary>
    /// Parses the string as a hexadecimal number, returning undefined when it is not one.
    /// </summary>
    [MondFunction]
    public static MondValue ParseHex(string str)
    {
        return long.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }
}

using System.Globalization;
using Mond.Binding;

namespace Mond.Libraries.Core;

[MondModule("Parse", bareMethods: true)]
internal static partial class ParseModule
{
    [MondFunction]
    public static MondValue ParseFloat(string str)
    {
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }

    [MondFunction]
    public static MondValue ParseInt(string str)
    {
        return long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }

    [MondFunction]
    public static MondValue ParseHex(string str)
    {
        return long.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result)
            ? result
            : MondValue.Undefined;
    }
}

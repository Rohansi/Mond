using System.Collections.Generic;
using System.Linq;

namespace Mond.SourceGenerator;

internal static class MondUtil
{
    private static readonly Dictionary<char, string> OperatorChars = new()
    {
        { '.', "Dot" },
        { '=', "Equals" },
        { '+', "Plus" },
        { '-', "Minus" },
        { '*', "Asterisk" },
        { '/', "Slash" },
        { '%', "Percent" },
        { '&', "Ampersand" },
        { '|', "Pipe" },
        { '^', "Caret" },
        { '~', "Tilde" },
        { '<', "LeftAngle" },
        { '>', "RightAngle" },
        { '!', "Bang" },
        { '?', "Question" },
        { '@', "At" },
        { '#', "Hash" },
        { '$', "Dollar" },
        { '\\', "Backslash" },
    };

    public static string GetOperatorIdentifier(string operatorToken)
    {
        var names = operatorToken.ToCharArray().Select(c => OperatorChars[c]);
        return $"op_{string.Join("", names)}";
    }
}

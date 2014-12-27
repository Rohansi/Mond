using System;
using System.Collections.Generic;

namespace Mond.Repl.Input
{
    public partial class Highlighter
    {
        private const ConsoleColor CommentColor = ConsoleColor.DarkGreen;
        private const ConsoleColor KeywordColor = ConsoleColor.Cyan;
        private const ConsoleColor IdentifierColor = ConsoleColor.White;
        private const ConsoleColor OperatorColor = ConsoleColor.Gray;
        private const ConsoleColor NumberColor = ConsoleColor.Magenta;
        private const ConsoleColor StringColor = ConsoleColor.Red;
        private const ConsoleColor OtherColor = ConsoleColor.Gray;

        private static readonly Dictionary<char, string[]> Operators;
        private static readonly HashSet<string> Keywords;
        private static readonly HashSet<char> HexChars;

        static Highlighter()
        {
            Operators = new Dictionary<char, string[]>
            {
                { ';', new [] { ";" } },
                { ',', new [] { "," } },
                { '.', new [] { "...", "." } },
                { '?', new [] { "?" } },
                { ':', new [] { ":" } },
    
                { '(', new [] { "(" } },
                { ')', new [] { ")" } },

                { '{', new [] { "{" } },
                { '}', new [] { "}" } },

                { '[', new [] { "[" } },
                { ']', new [] { "]" } },
    
                { '+', new [] { "++", "+=", "+" } },
                { '-', new [] { "->", "--", "-=", "-" } },
                { '*', new [] { "*=", "*" } },
                { '/', new [] { "/=", "/" } },
                { '%', new [] { "%=", "%" } },
    
                { '=', new [] { "==", "=" } },
                { '!', new [] { "!" } },
                { '>', new [] { ">=", ">" } },
                { '<', new [] { "<=", "<" } },
                { '&', new [] { "&&" } },
                { '|', new [] { "||", "|>" } },
            };

            Keywords = new HashSet<string>
            {
                "global",
                "undefined",
                "null",
                "true",
                "false",
                "NaN",
                "Infinity",

                "var",
                "const",
                "fun",
                "return",
                "seq",
                "yield",
                "if",
                "else",
                "for",
                "foreach",
                "in",
                "while",
                "do",
                "break",
                "continue",
                "switch",
                "case",
                "default",
            };

            HexChars = new HashSet<char>
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F',
                'a', 'b', 'c', 'd', 'e', 'f',
            };
        }
    }
}

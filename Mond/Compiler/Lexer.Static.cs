using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.Compiler
{
    partial class Lexer
    {
        private static HashSet<char> _punctuation;
        private static List<Tuple<string, TokenType>> _operators;
        private static Dictionary<string, TokenType> _keywords;

        static Lexer()
        {
            _operators = new List<Tuple<string, TokenType>>
            {
                Tuple.Create(";", TokenType.Semicolon),
                Tuple.Create(",", TokenType.Comma),
                Tuple.Create(".", TokenType.Dot),
                Tuple.Create("=", TokenType.Assign),

                Tuple.Create("(", TokenType.LeftParen),
                Tuple.Create(")", TokenType.RightParen),

                Tuple.Create("{", TokenType.LeftBrace),
                Tuple.Create("}", TokenType.RightBrace),

                Tuple.Create("[", TokenType.LeftSquare),
                Tuple.Create("]", TokenType.RightSquare),

                Tuple.Create("+", TokenType.Add),
                Tuple.Create("-", TokenType.Subtract),
                Tuple.Create("*", TokenType.Multiply),
                Tuple.Create("/", TokenType.Divide),
                Tuple.Create("%", TokenType.Modulo),
                Tuple.Create("++", TokenType.Increment),
                Tuple.Create("--", TokenType.Decrement),

                Tuple.Create("+=", TokenType.AddAssign),
                Tuple.Create("-=", TokenType.SubtractAssign),
                Tuple.Create("*=", TokenType.MultiplyAssign),
                Tuple.Create("/=", TokenType.DivideAssign),
                Tuple.Create("%=", TokenType.ModuloAssign),
                
                Tuple.Create("==", TokenType.EqualTo),
                Tuple.Create("!=", TokenType.NotEqualTo),
                Tuple.Create(">", TokenType.GreaterThan),
                Tuple.Create(">=", TokenType.GreaterThanOrEqual),
                Tuple.Create("<", TokenType.LessThan),
                Tuple.Create("<=", TokenType.LessThanOrEqual),
                Tuple.Create("!", TokenType.LogicalNot),
                Tuple.Create("&&", TokenType.LogicalAnd),
                Tuple.Create("||", TokenType.LogicalOr),

                Tuple.Create("?", TokenType.QuestionMark),
                Tuple.Create(":", TokenType.Colon),
                Tuple.Create("->", TokenType.Pointy),
                Tuple.Create("|>", TokenType.Pipeline)
            };

            _keywords = new Dictionary<string, TokenType>
            {
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "null", TokenType.Null },
                { "undefined", TokenType.Undefined },
                { "NaN", TokenType.NaN },
                { "Infinity", TokenType.Infinity },
                { "var", TokenType.Var },
                { "fun", TokenType.Fun },
                { "return", TokenType.Return },
                { "seq", TokenType.Seq },
                { "yield", TokenType.Yield },
                { "if", TokenType.If },
                { "else", TokenType.Else },
                { "for", TokenType.For },
                { "foreach", TokenType.Foreach },
                { "in", TokenType.In },
                { "while", TokenType.While },
                { "do", TokenType.Do },
                { "break", TokenType.Break },
                { "continue", TokenType.Continue },
                { "switch", TokenType.Switch },
                { "case", TokenType.Case },
                { "default", TokenType.Default },
            };

            // longest operators need to be first
            _operators = _operators.OrderByDescending(o => o.Item1.Length).ToList();

            // punctuation characters trigger operator detection, this should
            // contain the first character of each operator
            _punctuation = new HashSet<char>();

            foreach (var ch in _operators.Select(t => t.Item1[0]))
            {
                _punctuation.Add(ch);
            }
        }
    }
}

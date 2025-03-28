﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Mond.Compiler
{
    internal partial class Lexer
    {
        private static readonly OperatorDictionary _punctuation;
        private static readonly Dictionary<string, TokenType> _operators;
        private static readonly Dictionary<string, TokenType> _keywords;
        private static readonly HashSet<char> _hexChars;
        private static readonly Dictionary<char, string> _operatorChars;

        static Lexer()
        {
            _punctuation = new OperatorDictionary
            {
                { ";", TokenType.Semicolon, TokenSubType.Punctuation },
                { ",", TokenType.Comma, TokenSubType.Punctuation },

                { "(", TokenType.LeftParen, TokenSubType.Punctuation },
                { ")", TokenType.RightParen, TokenSubType.Punctuation },

                { "{", TokenType.LeftBrace, TokenSubType.Punctuation },
                { "}", TokenType.RightBrace, TokenSubType.Punctuation },

                { "[", TokenType.LeftSquare, TokenSubType.Punctuation },
                { "]", TokenType.RightSquare, TokenSubType.Punctuation },
                
                { ":", TokenType.Colon, TokenSubType.Punctuation },

                // Because of the way UDOs work, and how the lexer handles operators and punctuation
                // These two will never will never be returned by OperatorDictionary.Lookup()
                // But they are still lexed properly in Lexer.TryLexOperator().
                // They have been left in for clarity.
                { "->", TokenType.Pointy, TokenSubType.Punctuation },
                { "!in", TokenType.NotIn, TokenSubType.Operator }
            };

            _operators = new Dictionary<string, TokenType>
            {
                { ".", TokenType.Dot },
                { "=", TokenType.Assign },
                { "+", TokenType.Add },
                { "-", TokenType.Subtract },
                { "*", TokenType.Multiply },
                { "/", TokenType.Divide },
                { "%", TokenType.Modulo },
                { "**", TokenType.Exponent },
                { "&", TokenType.BitAnd },
                { "|", TokenType.BitOr },
                { "^", TokenType.BitXor },
                { "~", TokenType.BitNot },
                { "<<", TokenType.BitLeftShift },
                { ">>", TokenType.BitRightShift },
                { "++", TokenType.Increment },
                { "--", TokenType.Decrement },
                { "+=", TokenType.AddAssign },
                { "-=", TokenType.SubtractAssign },
                { "*=", TokenType.MultiplyAssign },
                { "/=", TokenType.DivideAssign },
                { "%=", TokenType.ModuloAssign },
                { "**=", TokenType.ExponentAssign },
                { "&=", TokenType.BitAndAssign },
                { "|=", TokenType.BitOrAssign },
                { "^=", TokenType.BitXorAssign },
                { "<<=", TokenType.BitLeftShiftAssign },
                { ">>=", TokenType.BitRightShiftAssign },
                { "==", TokenType.EqualTo },
                { "!=", TokenType.NotEqualTo },
                { ">", TokenType.GreaterThan },
                { ">=", TokenType.GreaterThanOrEqual },
                { "<", TokenType.LessThan },
                { "<=", TokenType.LessThanOrEqual },
                { "!", TokenType.Not },
                { "&&", TokenType.ConditionalAnd },
                { "||", TokenType.ConditionalOr },
                { "?", TokenType.QuestionMark },
                { "|>", TokenType.Pipeline },
                { "...", TokenType.Ellipsis },
                { "@", TokenType.Decorator }
            };

            _keywords = new Dictionary<string, TokenType>
            {
                { "global", TokenType.Global },
                { "undefined", TokenType.Undefined },
                { "null", TokenType.Null },
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "NaN", TokenType.NaN },
                { "Infinity", TokenType.Infinity },

                { "var", TokenType.Var },
                { "const", TokenType.Const },
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

                { "export", TokenType.Export },
                { "import", TokenType.Import },
                { "from", TokenType.From },

                { "debugger", TokenType.Debugger },

                { "__declare_globals", TokenType.DeclareGlobals },
            };

            _hexChars = new HashSet<char>
            {
                'a', 'b', 'c', 'd', 'e', 'f',
                'A', 'B', 'C', 'D', 'E', 'F',
            };

            // Note: A copy of this table is in Mond.SourceGenerator MondUtil.cs
            _operatorChars = new Dictionary<char, string>
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
        }

        public static bool IsOperatorToken(string s)
        {
            return s != null && s.All(_operatorChars.ContainsKey);
        }

        public static bool OperatorExists(string s)
        {
            return IsOperatorToken(s) && _operators.ContainsKey(s);
        }

        public static string GetOperatorIdentifier( string operatorToken )
        {
            var names = operatorToken.ToCharArray().Select( c => _operatorChars[c] );
            return $"op_{String.Join( String.Empty, names )}";
        }

        class OperatorDictionary : IEnumerable<KeyValuePair<char, List<Tuple<string, TokenType, TokenSubType>>>>
        {
            private readonly GenericComparer<Tuple<string, TokenType, TokenSubType>> _comparer; 
            private Dictionary<char, List<Tuple<string, TokenType, TokenSubType>>> _operatorDictionary;

            public OperatorDictionary()
            {
                _comparer = new GenericComparer<Tuple<string, TokenType, TokenSubType>>((a, b) => b.Item1.Length - a.Item1.Length);
                _operatorDictionary = new Dictionary<char, List<Tuple<string, TokenType, TokenSubType>>>();
            }

            public void Add(string op, TokenType type, TokenSubType subType)
            {
                if (!_operatorDictionary.TryGetValue(op[0], out var list))
                {
                    list = new List<Tuple<string, TokenType, TokenSubType>>();
                    _operatorDictionary.Add(op[0], list);
                }

                list.Add(Tuple.Create(op, type, subType));
                list.Sort(_comparer);
            }

            public IEnumerable<Tuple<string, TokenType, TokenSubType>> Lookup(char ch)
            {
                if (!_operatorDictionary.TryGetValue(ch, out var list))
                    return null;

                return list;
            }

            public IEnumerator<KeyValuePair<char, List<Tuple<string, TokenType, TokenSubType>>>> GetEnumerator()
            {
                var enumerator = _operatorDictionary.GetEnumerator();

                while (enumerator.MoveNext())
                    yield return enumerator.Current;

                enumerator.Dispose();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

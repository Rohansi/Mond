using System;
using System.Collections;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class Lexer
    {
        private static OperatorDictionary _operators;
        private static Dictionary<string, TokenType> _keywords;
        private static HashSet<char> _hexChars;

        static Lexer()
        {
            _operators = new OperatorDictionary
            {
                { ";", TokenType.Semicolon, TokenSubType.None },
                { ",", TokenType.Comma, TokenSubType.None },
                { ".", TokenType.Dot, TokenSubType.Operator },
                { "=", TokenType.Assign, TokenSubType.Operator },

                { "(", TokenType.LeftParen, TokenSubType.None },
                { ")", TokenType.RightParen, TokenSubType.None },

                { "{", TokenType.LeftBrace, TokenSubType.None },
                { "}", TokenType.RightBrace, TokenSubType.None },

                { "[", TokenType.LeftSquare, TokenSubType.None },
                { "]", TokenType.RightSquare, TokenSubType.None },

                { "+", TokenType.Add, TokenSubType.Operator },
                { "-", TokenType.Subtract, TokenSubType.Operator },
                { "*", TokenType.Multiply, TokenSubType.Operator },
                { "/", TokenType.Divide, TokenSubType.Operator },
                { "%", TokenType.Modulo, TokenSubType.Operator },
                { "**", TokenType.Exponent, TokenSubType.Operator },
                { "&", TokenType.BitAnd, TokenSubType.Operator },
                { "|", TokenType.BitOr, TokenSubType.Operator },
                { "^", TokenType.BitXor, TokenSubType.Operator },
                { "~", TokenType.BitNot, TokenSubType.Operator },
                { "<<", TokenType.BitLeftShift, TokenSubType.Operator },
                { ">>", TokenType.BitRightShift, TokenSubType.Operator },
                { "++", TokenType.Increment, TokenSubType.Operator },
                { "--", TokenType.Decrement, TokenSubType.Operator },

                { "+=", TokenType.AddAssign, TokenSubType.Operator },
                { "-=", TokenType.SubtractAssign, TokenSubType.Operator },
                { "*=", TokenType.MultiplyAssign, TokenSubType.Operator },
                { "/=", TokenType.DivideAssign, TokenSubType.Operator },
                { "%=", TokenType.ModuloAssign, TokenSubType.Operator },
                { "**=", TokenType.ExponentAssign, TokenSubType.Operator },
                { "&=", TokenType.BitAndAssign, TokenSubType.Operator },
                { "|=", TokenType.BitOrAssign, TokenSubType.Operator },
                { "^=", TokenType.BitXorAssign, TokenSubType.Operator },
                { "<<=", TokenType.BitLeftShiftAssign, TokenSubType.Operator },
                { ">>=", TokenType.BitRightShiftAssign, TokenSubType.Operator },
                
                { "==", TokenType.EqualTo, TokenSubType.Operator },
                { "!=", TokenType.NotEqualTo, TokenSubType.Operator },
                { ">", TokenType.GreaterThan, TokenSubType.Operator },
                { ">=", TokenType.GreaterThanOrEqual, TokenSubType.Operator },
                { "<", TokenType.LessThan, TokenSubType.Operator },
                { "<=", TokenType.LessThanOrEqual, TokenSubType.Operator },
                { "!", TokenType.Not, TokenSubType.Operator },
                { "&&", TokenType.ConditionalAnd, TokenSubType.Operator },
                { "||", TokenType.ConditionalOr, TokenSubType.Operator },

                { "?", TokenType.QuestionMark, TokenSubType.Operator },
                { ":", TokenType.Colon, TokenSubType.Operator },
                { "->", TokenType.Pointy, TokenSubType.Operator },
                { "|>", TokenType.Pipeline, TokenSubType.Operator },
                { "...", TokenType.Ellipsis, TokenSubType.Operator },
                { "!in", TokenType.NotIn, TokenSubType.None }
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

                { "debugger", TokenType.Debugger },
            };

            _hexChars = new HashSet<char>
            {
                'a', 'b', 'c', 'd', 'e', 'f',
                'A', 'B', 'C', 'D', 'E', 'F',
            };
        }

        class OperatorDictionary : IEnumerable<object>
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
                List<Tuple<string, TokenType, TokenSubType>> list;
                if (!_operatorDictionary.TryGetValue(op[0], out list))
                {
                    list = new List<Tuple<string, TokenType, TokenSubType>>();
                    _operatorDictionary.Add(op[0], list);
                }

                list.Add(Tuple.Create(op, type, subType));
                list.Sort(_comparer);
            }

            public IEnumerable<Tuple<string, TokenType, TokenSubType>> Lookup(char ch)
            {
                List<Tuple<string, TokenType, TokenSubType>> list;
                if (!_operatorDictionary.TryGetValue(ch, out list))
                    return null;

                return list;
            }

            public IEnumerator<object> GetEnumerator()
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

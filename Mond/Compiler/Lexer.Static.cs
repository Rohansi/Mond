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
                { ";", TokenType.Semicolon },
                { ",", TokenType.Comma },
                { ".", TokenType.Dot },
                { "=", TokenType.Assign },

                { "(", TokenType.LeftParen },
                { ")", TokenType.RightParen },

                { "{", TokenType.LeftBrace },
                { "}", TokenType.RightBrace },

                { "[", TokenType.LeftSquare },
                { "]", TokenType.RightSquare },

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
                { ":", TokenType.Colon },
                { "->", TokenType.Pointy },
                { "|>", TokenType.Pipeline },
                { "...", TokenType.Ellipsis }
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
            };

            _hexChars = new HashSet<char>
            {
                'a', 'b', 'c', 'd', 'e', 'f',
                'A', 'B', 'C', 'D', 'E', 'F',
            };
        }

        class OperatorDictionary : IEnumerable<object>
        {
            private readonly GenericComparer<Tuple<string, TokenType>> _comparer; 
            private Dictionary<char, List<Tuple<string, TokenType>>> _operatorDictionary;

            public OperatorDictionary()
            {
                _comparer = new GenericComparer<Tuple<string, TokenType>>((a, b) => b.Item1.Length - a.Item1.Length);
                _operatorDictionary = new Dictionary<char, List<Tuple<string, TokenType>>>();
            }

            public void Add(string op, TokenType type)
            {
                List<Tuple<string, TokenType>> list;
                if (!_operatorDictionary.TryGetValue(op[0], out list))
                {
                    list = new List<Tuple<string, TokenType>>();
                    _operatorDictionary.Add(op[0], list);
                }

                list.Add(Tuple.Create(op, type));
                list.Sort(_comparer);
            }

            public IEnumerable<Tuple<string, TokenType>> Lookup(char ch)
            {
                List<Tuple<string, TokenType>> list;
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

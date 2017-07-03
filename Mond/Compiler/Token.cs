namespace Mond.Compiler
{
    enum TokenSubType
    {
        None,
        Keyword,
        Operator,
        Punctuation,
    }

    enum TokenType
    {
        Identifier,
        BacktickIdentifier,
        Number,
        String,

        Global,
        Undefined,
        Null,
        True,
        False,
        NaN,
        Infinity,

        Var,
        Const,
        Fun,
        Return,
        Seq,
        Yield,
        If,
        Else,
        For,
        Foreach,
        In,
        NotIn,
        While,
        Do,
        Break,
        Continue,
        Switch,
        Case,
        Default,

        Debugger,

        Semicolon,
        Comma,
        Dot,
        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        ModuloAssign,
        ExponentAssign,
        BitLeftShiftAssign,
        BitRightShiftAssign,
        BitOrAssign,
        BitAndAssign,
        BitXorAssign,

        LeftParen,
        RightParen,

        LeftBrace,
        RightBrace,

        LeftSquare,
        RightSquare,

        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Exponent,
        Increment,
        Decrement,
        BitLeftShift,
        BitRightShift,
        BitAnd,
        BitOr,
        BitXor,
        BitNot,

        Not,
        EqualTo,
        NotEqualTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        ConditionalAnd,
        ConditionalOr,

        QuestionMark,
        Colon,
        Pointy,
        Pipeline,
        Ellipsis,
        UserDefinedOperator,
        Decorator,

        Eof
    }

    class Token
    {
        public string FileName { get; }
        public int Line { get; }
        public int Column { get; }
        public TokenType Type { get; }
        public TokenSubType SubType { get; }
        public string Contents { get; }
        public object Tag { get; }

        public Token(string fileName, int line, int column, TokenType type, string contents, TokenSubType subType = TokenSubType.None, object tag = null)
        {
            FileName = fileName;
            Line = line;
            Column = column;
            Type = type;
            SubType = subType;
            Contents = contents;
            Tag = tag;
        }

        public Token(Token token, TokenType type, string contents, TokenSubType subType = TokenSubType.None, object tag = null)
            : this(token.FileName, token.Line, token.Column, type, contents, subType, tag)
        {

        }

        public override string ToString()
        {
            switch (Type)
            {
                case TokenType.Identifier:
                case TokenType.Number:
                case TokenType.String:
                    var contentsStr = Contents;
                    if (contentsStr.Length > 16)
                        contentsStr = contentsStr.Substring(0, 13) + "...";

                    return string.Format("{0}('{1}')", Type, contentsStr);

                default:
                    return string.Format("{0}({1})", Type, SubType);
            }
        }
    }
}

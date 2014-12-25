namespace Mond.Compiler
{
    enum TokenType
    {
        Identifier,
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

        Eof
    }

    class Token
    {
        public readonly string FileName;
        public readonly int Line;
        public readonly int Column;
        public readonly TokenType Type;
        public readonly string Contents;
        public readonly object Tag;

        public Token(string fileName, int line, int column, TokenType type, string contents, object tag = null)
        {
            FileName = fileName;
            Line = line;
            Column = column;
            Type = type;
            Contents = contents;
            Tag = tag;
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
                    return Type.ToString();
            }
        }
    }
}

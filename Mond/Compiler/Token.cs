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
        public readonly TokenType Type;
        public readonly string Contents;

        public Token(string fileName, int line, TokenType type, string contents)
        {
            FileName = fileName;
            Line = line;
            Type = type;
            Contents = contents;
        }
    }
}

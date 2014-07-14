namespace Mond.Compiler
{
    enum TokenType
    {
        Identifier,
        Number,
        String,
        Undefined,
        Null,
        True,
        False,
        NaN,
        Infinity,

        Var,
        Fun,
        Return,
        If,
        Else,
        For,
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
        Increment,
        Decrement,

        EqualTo,
        NotEqualTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        LogicalAnd,
        LogicalOr,
        LogicalNot,

        QuestionMark,
        Colon,
        Pointy,

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

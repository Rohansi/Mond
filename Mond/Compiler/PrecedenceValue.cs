namespace Mond.Compiler
{
    enum PrecedenceValue
    {
        Invalid = 0,
        Assign = 1,
        Ternary,
        ConditionalOr,
        ConditionalAnd,
        Equality,
        Relational,
        Addition,
        Multiplication,
        Prefix,
        Postfix
    }
}

namespace Mond.Compiler
{
    enum PrecedenceValue
    {
        Invalid = 0,
        Assign = 1,
        Ternary,
        LogicalAndOr,
        Equality,
        Relational,
        Addition,
        Multiplication,
        Prefix,
        Postfix
    }
}

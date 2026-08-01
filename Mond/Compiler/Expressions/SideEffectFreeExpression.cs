namespace Mond.Compiler.Expressions
{
    /// <summary>
    /// Determines if an expression can be evaluated without running any user code or otherwise
    /// observing/modifying program state. This lets the compiler reorder the evaluation of an
    /// expression relative to other operations.
    /// </summary>
    static class SideEffectFreeExpression
    {
        public static bool Check(Expression expression, FunctionContext context)
        {
            switch (expression)
            {
                // constants are always safe
                case IConstantExpression:
                    return true;

                // reading a local or argument cannot run code - globals can, because the global
                // object may be a proxy or have metamethods
                case IdentifierExpression identifier:
                    return context.TryGetIdentifier(identifier.Name, out _);

                default:
                    return false;
            }
        }
    }
}

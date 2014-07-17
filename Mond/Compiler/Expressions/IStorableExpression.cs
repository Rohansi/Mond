namespace Mond.Compiler.Expressions
{
    interface IStorableExpression
    {
        int CompileStore(FunctionContext context);
    }
}

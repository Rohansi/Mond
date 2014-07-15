namespace Mond.Compiler.Expressions
{
    interface IStorableExpression
    {
        void CompileStore(FunctionContext context);
    }
}

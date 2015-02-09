namespace Mond.Compiler.Expressions
{
    interface IStorableExpression
    {
        int CompilePreLoadStore(FunctionContext context, int times);
        int CompileLoad(FunctionContext context);
        int CompileStore(FunctionContext context);
    }
}

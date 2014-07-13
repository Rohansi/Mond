namespace Mond.Compiler.Expressions
{
    interface IStorableExpression
    {
        void CompileStore(CompilerContext context);
    }
}

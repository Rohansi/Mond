namespace Mond.Compiler.Expressions
{
    interface IConstantExpression
    {
        /// <summary>
        /// Used by SwitchExpression
        /// </summary>
        MondValue GetValue();
    }
}

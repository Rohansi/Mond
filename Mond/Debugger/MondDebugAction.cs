namespace Mond.Debugger
{
    public enum MondDebugAction
    {
        /// <summary>
        /// Run the program normally.
        /// </summary>
        Run,

        /// <summary>
        /// Runs until a function is called or another statement starts.
        /// </summary>
        StepInto,

        /// <summary>
        /// Runs until the current function returns.
        /// </summary>
        StepOut,

        /// <summary>
        /// Runs until a function is called and returned from or another statement starts.
        /// </summary>
        StepOver
    }
}

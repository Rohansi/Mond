namespace Mond.Debugger
{
    public enum MondDebugAction
    {
        /// <summary>
        /// Run the program normally.
        /// </summary>
        Run,

        /// <summary>
        /// Runs until another statement starts.
        /// </summary>
        StepInto,

        /// <summary>
        /// Runs until another statement starts in the current function.
        /// </summary>
        StepOver,

        /// <summary>
        /// Runs until the current function returns.
        /// </summary>
        StepOut
    }
}

namespace Mond
{
    public class MondCompilerOptions
    {
        /// <summary>
        /// Generate debug information for stack traces.
        /// </summary>
        public bool GenerateDebugInfo { get; set; }

        /// <summary>
        /// Force all declarations in the script root to be stored in the global object.
        /// </summary>
        public bool MakeRootDeclarationsGlobal { get; set; }

        /// <summary>
        /// Make undefined variables resolve to globals.
        /// </summary>
        public bool UseImplicitGlobals { get; set; }

        /// <summary>
        /// Controls the starting line number for scripts. This should only be used when
        /// injecting code into scripts as it allows you to correct the line numbers.
        /// </summary>
        public int FirstLineNumber { get; set; }

        public MondCompilerOptions()
        {
            GenerateDebugInfo = true;
            MakeRootDeclarationsGlobal = false;
            UseImplicitGlobals = false;
            FirstLineNumber = 1;
        }
    }
}

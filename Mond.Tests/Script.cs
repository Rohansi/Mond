using Mond.Libraries;

namespace Mond.Tests
{
    static class Script
    {
        private static readonly MondLibraryManager Libraries = new MondLibraryManager
        {
            new CoreLibraries()
        };

        private static readonly MondCompilerOptions Options = new MondCompilerOptions
        {
            FirstLineNumber = 0
        };

        public static MondValue Run(string source)
        {
            var state = new MondState();
            Libraries.Load(state);

            var program = MondProgram.Compile(Libraries.Definitions + source, null, Options);
            return state.Load(program);
        }

        public static MondValue Run(out MondState state, string source)
        {
            state = new MondState();
            Libraries.Load(state);

            var program = MondProgram.Compile(Libraries.Definitions + source, null, Options);
            return state.Load(program);
        }

        public static MondValue Run(this MondState state, string source)
        {
            Libraries.Load(state);

            var program = MondProgram.Compile(Libraries.Definitions + source, null, Options);
            return state.Load(program);
        }

        public static MondState Load(params string[] sources)
        {
            var state = new MondState();
            Libraries.Load(state);

            foreach (var source in sources)
            {
                var program = MondProgram.Compile(Libraries.Definitions + source, null, Options);
                state.Load(program);
            }

            return state;
        }
    }
}

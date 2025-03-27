using Mond.Libraries;

namespace Mond.Tests
{
    internal static class Script
    {
        public static MondState NewState()
        {
            return new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full, // enables additional compiler code paths
                },
                Libraries = new MondLibraryManager
                {
                    new CoreLibraries()
                },
            };
        }

        public static MondValue Run(string source)
        {
            var state = NewState();
            return state.Run(source);
        }

        public static MondValue Run(out MondState state, string source)
        {
            state = NewState();
            return state.Run(source);
        }

        public static MondState Load(params string[] sources)
        {
            var state = NewState();

            foreach (var source in sources)
            {
                state.Run(source);
            }

            return state;
        }
    }
}

namespace Mond.Tests
{
    static class Script
    {
        public static MondValue Run(string source)
        {
            var state = new MondState();
            var program = MondProgram.Compile(source);
            return state.Load(program);
        }

        public static MondValue Run(out MondState state, string source)
        {
            state = new MondState();
            var program = MondProgram.Compile(source);
            return state.Load(program);
        }

        public static MondState Load(params string[] sources)
        {
            var state = new MondState();

            foreach (var source in sources)
            {
                var program = MondProgram.Compile(source);
                state.Load(program);
            }

            return state;
        }
    }
}

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
    }
}

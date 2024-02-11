using System.Collections.Generic;
using System.IO;
using Mond.Libraries.Console;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains all of the console related libraries.
    /// </summary>
    public class ConsoleLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new ConsoleOutputLibrary();
            yield return new ConsoleInputLibrary();
        }
    }

    /// <summary>
    /// Library containing the <c>print</c> functions.
    /// </summary>
    public class ConsoleOutputLibrary : IMondLibrary
    {
        public TextWriter Out { get; set; }

        public ConsoleOutputLibrary()
        {
            Out = System.Console.Out;
        }

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            return new ConsoleOutput.Library(new ConsoleOutput(this)).GetDefinitions(state);
        }
    }

    /// <summary>
    /// Library containing the <c>readLn</c> function.
    /// </summary>
    public class ConsoleInputLibrary : IMondLibrary
    {
        public TextReader In { get; set; }

        public ConsoleInputLibrary()
        {
            In = System.Console.In;
        }

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            return new ConsoleInput.Library(new ConsoleInput(this)).GetDefinitions(state);
        }
    }
}

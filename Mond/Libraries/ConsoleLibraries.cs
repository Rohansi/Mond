using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var libraries = new IMondLibraryCollection[]
            {
                new ConsoleOutputLibraries(),
                new ConsoleInputLibraries()
            };

            return libraries.SelectMany(l => l.Create(state));
        }
    }

    /// <summary>
    /// Contains the console output libraries.
    /// </summary>
    public class ConsoleOutputLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new ConsoleOutputLibrary();
        }
    }

    /// <summary>
    /// Contains the console input libraries.
    /// </summary>
    public class ConsoleInputLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
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
            var library = new ConsoleOutput.Library(new ConsoleOutput(this));
            foreach (var t in library.GetDefinitions(state))
            {
                yield return t;
            }
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
            var library = new ConsoleInput.Library(new ConsoleInput(this));
            foreach (var t in library.GetDefinitions(state))
            {
                yield return t;
            }
        }
    }
}

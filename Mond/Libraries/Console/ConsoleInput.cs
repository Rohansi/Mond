using System;
using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondModule("ConsoleInput", bareMethods: true)]
    internal partial class ConsoleInput
    {
        private readonly ConsoleInputLibrary _consoleInput;

        public ConsoleInput(ConsoleInputLibrary consoleInput)
        {
            _consoleInput = consoleInput ?? throw new ArgumentNullException(nameof(consoleInput));
        }

        /// <summary>
        /// Reads a line of text from the console, or null at the end of the input.
        /// </summary>
        [MondFunction]
        public string ReadLn()
        {
            return _consoleInput.In.ReadLine();
        }
    }
}

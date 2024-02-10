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

        [MondFunction]
        public string ReadLn()
        {
            return _consoleInput.In.ReadLine();
        }
    }
}

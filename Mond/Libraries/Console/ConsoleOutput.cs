using System;
using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondModule("ConsoleOutput", bareMethods: true)]
    internal partial class ConsoleOutput
    {
        private readonly ConsoleOutputLibrary _consoleOutput;

        public ConsoleOutput(ConsoleOutputLibrary consoleOutput)
        {
            _consoleOutput = consoleOutput ?? throw new ArgumentNullException(nameof(consoleOutput));
        }

        /// <summary>
        /// Writes each argument to the console as a string.
        /// </summary>
        [MondFunction]
        public void Print(params Span<MondValue> arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }
        }

        /// <summary>
        /// Writes each argument to the console as a string, followed by a line break.
        /// </summary>
        [MondFunction]
        public void PrintLn(params Span<MondValue> arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }

            _consoleOutput.Out.WriteLine();
        }
    }
}

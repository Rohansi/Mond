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

        [MondFunction]
        public void Print(params Span<MondValue> arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }
        }

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

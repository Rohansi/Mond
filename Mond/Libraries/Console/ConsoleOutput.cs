using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondClass("ConsoleOutput")]
    internal class ConsoleOutputClass
    {
        private ConsoleOutputLibrary _consoleOutput;

        public static MondValue Create(MondState state, ConsoleOutputLibrary consoleOutput)
        {
            MondValue prototype;
            MondClassBinder.Bind<ConsoleOutputClass>(state, out prototype);

            var instance = new ConsoleOutputClass();
            instance._consoleOutput = consoleOutput;

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction]
        public void Print(params MondValue[] arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }
        }

        [MondFunction]
        public void PrintLn(params MondValue[] arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }

            _consoleOutput.Out.WriteLine();
        }
    }
}

using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondClass("")]
    internal class ConsoleOutputClass
    {
        private ConsoleOutputLibrary _consoleOutput;

        public static MondValue Create(ConsoleOutputLibrary consoleOutput)
        {
            MondValue prototype;
            MondClassBinder.Bind<ConsoleOutputClass>(out prototype);

            var instance = new ConsoleOutputClass();
            instance._consoleOutput = consoleOutput;

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("print")]
        public void Print(params MondValue[] arguments)
        {
            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
            }
        }

        [MondFunction("printLn")]
        public void PrintLn(params MondValue[] arguments)
        {
            if (arguments.Length == 0)
                _consoleOutput.Out.WriteLine();

            foreach (var v in arguments)
            {
                _consoleOutput.Out.Write((string)v);
                _consoleOutput.Out.WriteLine();
            }
        }
    }
}

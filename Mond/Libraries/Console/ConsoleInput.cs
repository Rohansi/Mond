using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondClass("")]
    internal class ConsoleInputClass
    {
        private ConsoleInputLibrary _consoleInput;

        public static MondValue Create(ConsoleInputLibrary consoleInput)
        {
            MondValue prototype;
            MondClassBinder.Bind<ConsoleInputClass>(out prototype);

            var instance = new ConsoleInputClass();
            instance._consoleInput = consoleInput;

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("readLn")]
        public string ReadLn()
        {
            return _consoleInput.In.ReadLine();
        }
    }
}

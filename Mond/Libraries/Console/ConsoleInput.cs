using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondClass("ConsoleInput")]
    internal partial class ConsoleInputClass
    {
        private ConsoleInputLibrary _consoleInput;

        public static MondValue Create(MondState state, ConsoleInputLibrary consoleInput)
        {
            MondValue prototype;
            MondClassBinder.Bind<ConsoleInputClass>(state, out prototype);

            var instance = new ConsoleInputClass();
            instance._consoleInput = consoleInput;

            var obj = MondValue.Object();
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction]
        public string ReadLn()
        {
            return _consoleInput.In.ReadLine();
        }
    }
}

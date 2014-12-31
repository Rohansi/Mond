using Mond.Binding;

namespace Mond.Libraries.Console
{
    [MondClass("")]
    internal class MondConsoleInput
    {
        private ConsoleInputLibrary _consoleInput;

        public static MondValue Create(ConsoleInputLibrary consoleInput)
        {
            MondValue prototype;
            MondClassBinder.Bind<MondConsoleInput>(out prototype);

            var instance = new MondConsoleInput();
            instance._consoleInput = consoleInput;

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("readLn")]
        public MondValue ReadLn()
        {
            var line = _consoleInput.In.ReadLine();

            if (line == null)
                return MondValue.Null;

            return line;
        }
    }
}

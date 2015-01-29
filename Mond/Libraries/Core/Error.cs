using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("")]
    internal class ErrorModule
    {
        [MondFunction("error")]
        public static void Error(string message)
        {
            throw new MondRuntimeException(message);
        }
    }
}

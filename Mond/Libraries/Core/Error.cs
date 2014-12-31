using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("")]
    internal class MondError
    {
        [MondFunction("error")]
        public static void Error(string message)
        {
            throw new MondRuntimeException(message);
        }
    }
}

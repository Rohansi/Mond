using System.IO;
using System.Text;
using Mond.Libraries;
using NUnit.Framework;

namespace Mond.Tests.Libraries
{
    public class LibraryManagerTests
    {
        [TestCase]
        public void AddIndividualLibrary()
        {
            var state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
                Libraries = new MondLibraryManager
                {
                    new ConsoleOutputLibrary(),
                },
            };

            var sb = new StringBuilder();
            state.Libraries.Configure(libraries =>
            {
                libraries.Get<ConsoleOutputLibrary>().Out = new StringWriter(sb);
            });

            state.Run("print('ok');");

            Assert.AreEqual("ok", sb.ToString());
        }
    }
}

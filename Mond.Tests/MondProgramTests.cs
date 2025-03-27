using System.IO;
using NUnit.Framework;

namespace Mond.Tests
{
    [TestFixture]
    public class MondProgramTests
    {
        [Test]
        public void SerializeProgram()
        {
            const string code =
                """
                fun test(x, y) {
                    return x + y;
                }
                
                return test(10, 5);
                """;

            var originalProgram = MondProgram.Compile(code, options: new MondCompilerOptions { DebugInfo = MondDebugInfoLevel.Full });

            var ms = new MemoryStream();
            originalProgram.SaveBytecode(ms);

            ms.Position = 0;
            var loadedProgram = MondProgram.LoadBytecode(ms);

            var state = new MondState();
            var result = state.Call(loadedProgram.EntryPoint);

            Assert.AreEqual((MondValue)15, result);
        }
    }
}

                
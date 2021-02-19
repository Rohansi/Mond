using System.IO;
using Mond.Libraries;
using NUnit.Framework;

namespace Mond.Tests.Libraries
{
    [TestFixture]
    public class RequireLibraryTests
    {
        [Test]
        public void Require()
        {
            var mainPath = "/test/main.mnd";
            var searchPath = Path.GetDirectoryName(mainPath);

            var configured = false;
            var state = new MondState();

            state.Libraries.Configure(libraries =>
            {
                var requireLibrary = libraries.Get<RequireLibrary>();
                Assert.IsNotNull(requireLibrary);

                requireLibrary.Resolver = (name, directories) =>
                {
                    Assert.AreEqual("module", name);
                    CollectionAssert.Contains(directories, searchPath);

                    return "resolved-module";
                };

                requireLibrary.Loader = resolvedName =>
                {
                    Assert.AreEqual("resolved-module", resolvedName);
                    return "exports.foo = 'bar';";
                };

                configured = true;
            });

            var result = state.Run("return require('module').foo;", mainPath);

            Assert.IsTrue(configured, "Configure was not called");
            Assert.AreEqual((MondValue)"bar", result);
        }
    }
}

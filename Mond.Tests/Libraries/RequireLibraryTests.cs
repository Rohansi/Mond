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
            var result = RunModule(
                "return require('module').foo;",
                "module",
                "exports.foo = 'bar';"
            );
            
            Assert.That(result, Is.EqualTo((MondValue)"bar"));
        }

        [Test]
        public void Import()
        {
            const string mainScript =
                """
                import Module;
                return Module.method();
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            var result = RunModule(mainScript, "Module", moduleScript);
            Assert.That(result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void ImportInvalidModuleName()
        {
            const string mainScript =
                """
                import module;
                return module.method();
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            Assert.Throws<MondCompilerException>(() => RunModule(mainScript, "module", moduleScript));
        }

        [Test]
        public void ImportInvalidIdentifier()
        {
            const string mainScript =
                """
                import '123';
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            Assert.Throws<MondCompilerException>(() => RunModule(mainScript, "123", moduleScript));
        }

        [Test]
        public void ImportString()
        {
            const string mainScript =
                """
                import 'Module.mnd';
                return Module.method();
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            var result = RunModule(mainScript, "Module.mnd", moduleScript);
            Assert.That(result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void ImportDestructured()
        {
            const string mainScript =
                """
                from Module import { method };
                return method();
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            var result = RunModule(mainScript, "Module", moduleScript);
            Assert.That(result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void ImportStringDestructured()
        {
            const string mainScript =
                """
                from 'module.mnd' import { method };
                return method();
                """;
            const string moduleScript =
                """
                export fun method() {
                    return 10;
                }
                """;
            var result = RunModule(mainScript, "module.mnd", moduleScript);
            Assert.That(result, Is.EqualTo((MondValue)10));
        }

        [Test]
        public void ExportAll()
        {
            const string mainScript =
                """
                return fun (exports) {
                    export * from 'module.mnd';
                };
                """;
            const string moduleScript =
                """
                export fun methodA() {
                    return 10;
                }
                export fun methodB() {
                    return 20;
                }
                """;
            var module = RunModule(mainScript, "module.mnd", moduleScript, out var state);
            var exports = MondValue.Object(state);
            state.Call(module, exports);

            var methodA = exports["methodA"];
            Assert.That(methodA.Type, Is.EqualTo(MondValueType.Function));
            var resultA = state.Call(methodA);
            Assert.That(resultA, Is.EqualTo((MondValue)10));

            var methodB = exports["methodB"];
            Assert.That(methodB.Type, Is.EqualTo(MondValueType.Function));
            var resultB = state.Call(methodB);
            Assert.That(resultB, Is.EqualTo((MondValue)20));
        }

        private static MondValue RunModule(string mainScript, string moduleName, string moduleScript)
        {
            return RunModule(mainScript, moduleName, moduleScript, out _);
        }

        private static MondValue RunModule(string mainScript, string moduleName, string moduleScript, out MondState state)
        {
            const string mainPath = "/test/main.mnd";
            var searchPath = Path.GetDirectoryName(mainPath);

            var configured = false;
            state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
            };

            state.Libraries.Configure(libraries =>
            {
                var requireLibrary = libraries.Get<RequireLibrary>();
                Assert.That(requireLibrary, Is.Not.Null);

                requireLibrary.Resolver = (name, directories) =>
                {
                    Assert.That(name, Is.EqualTo(moduleName));
                    Assert.That(directories, Does.Contain(searchPath));

                    return "resolved-module";
                };

                requireLibrary.Loader = resolvedName =>
                {
                    Assert.That(resolvedName, Is.EqualTo("resolved-module"));
                    return moduleScript;
                };

                configured = true;
            });

            var result = state.Run(mainScript, mainPath);
            Assert.That(configured, Is.True, "Configure was not called");
            return result;
        }
    }
}

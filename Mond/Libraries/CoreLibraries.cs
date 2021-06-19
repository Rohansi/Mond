using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Binding;
using Mond.Libraries.Core;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains the basic libraries that should be supported everywhere. This
    /// includes the <c>error</c>, <c>try</c> and <c>require</c> functions, the
    /// <c>Char</c> and <c>Math</c> modules, and the <c>Random</c> class.
    /// </summary>
    public class CoreLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new ErrorLibrary();
            yield return new RequireLibrary();
            yield return new CharLibrary();
            yield return new MathLibrary();
            yield return new RandomLibrary();
            yield return new OperatorLibrary();
        }
    }

    /// <summary>
    /// Library containing the <c>error</c> and <c>try</c> functions.
    /// </summary>
    public class ErrorLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var errorModule = MondModuleBinder.Bind<ErrorModule>(state);
            yield return new KeyValuePair<string, MondValue>("error", errorModule["error"]);
            yield return new KeyValuePair<string, MondValue>("try", errorModule["try"]);
        }
    }

    /// <summary>
    /// Library containing the <c>require</c> function.
    /// </summary>
    public class RequireLibrary : IMondLibrary
    {
        public delegate string ModuleResolver(string name, IEnumerable<string> searchDirectories);
        public delegate string ModuleLoader(string resolvedName);
        
        /// <summary>
        /// The options to use when compiling modules. <c>FirstLineNumber</c> will be set to its proper value.
        /// </summary>
        public MondCompilerOptions Options { get; set; }

        /// <summary>
        /// The definition string from <c>MondLibraryManager</c>. This shouldn't need to be changed.
        /// </summary>
        public string Definitions { get; set; }

        /// <summary>
        /// Directories that the <c>ModuleLoader</c> should search.
        /// </summary>
        public IReadOnlyList<string> SearchDirectories { get; set; }

        /// <summary>
        /// Includes the current script's directory in search directories.
        /// </summary>
        public bool SearchBesideScript { get; set; }

        /// <summary>
        /// The function used to resolve a module into a name the loader can load.
        /// Modules will be cached based on their resolved names.
        /// For example, this may turn a relative path into an absolute path.
        /// </summary>
        public ModuleResolver Resolver { get; set; }

        /// <summary>
        /// The function used to load a module using its resolved name.
        /// </summary>
        public ModuleLoader Loader { get; set; }

        public RequireLibrary()
        {
            Definitions = "\n";
            SearchDirectories = new[] { "." };
            SearchBesideScript = true;

            Resolver = (name, searchDirectories) =>
            {
                var foundModule = searchDirectories
                    .Where(p => p != null)
                    .Select(p => Path.Combine(p, name))
                    .FirstOrDefault(File.Exists);

                if (foundModule == null)
                    throw new MondRuntimeException("require: module could not be found: {0}", name);

                return Path.GetFullPath(foundModule);
            };

            Loader = File.ReadAllText;
        } 

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var requireClass = RequireClass.Create(state, this);
            yield return new KeyValuePair<string, MondValue>("require", requireClass["require"]);
        }
    }

    /// <summary>
    /// Library containing the <c>Char</c> module.
    /// </summary>
    public class CharLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var charModule = MondModuleBinder.Bind<CharModule>(state);
            yield return new KeyValuePair<string, MondValue>("Char", charModule);
        }
    }

    /// <summary>
    /// Library containing the <c>Math</c> module.
    /// </summary>
    public class MathLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var mathModule = MondModuleBinder.Bind<MathModule>(state);

            mathModule["PI"] = System.Math.PI;
            mathModule["E"] = System.Math.E;

            yield return new KeyValuePair<string, MondValue>("Math", mathModule);
        }
    }

    /// <summary>
    /// Library containing the built-in operators.
    /// </summary>
    public class OperatorLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var operatorModule = MondModuleBinder.Bind<OperatorModule>(state);
            foreach (var pair in operatorModule.AsDictionary)
                yield return new KeyValuePair<string, MondValue>(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Library containing the <c>Random</c> class.
    /// </summary>
    public class RandomLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            var randomClass = MondClassBinder.Bind<RandomClass>(state);
            yield return new KeyValuePair<string, MondValue>("Random", randomClass);
        }
    }
}

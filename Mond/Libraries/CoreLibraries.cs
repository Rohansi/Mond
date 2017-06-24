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
            yield return new ErrorLibrary(state);
            yield return new RequireLibrary(state);
            yield return new CharLibrary(state);
            yield return new MathLibrary(state);
            yield return new RandomLibrary(state);
            yield return new OperatorLibrary(state);
        }
    }

    /// <summary>
    /// Library containing the <c>error</c> and <c>try</c> functions.
    /// </summary>
    public class ErrorLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public ErrorLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var errorModule = MondModuleBinder.Bind<ErrorModule>(_state);
            yield return new KeyValuePair<string, MondValue>("error", errorModule["error"]);
            yield return new KeyValuePair<string, MondValue>("try", errorModule["try"]);
        }
    }

    /// <summary>
    /// Library containing the <c>require</c> function.
    /// </summary>
    public class RequireLibrary : IMondLibrary
    {
        public delegate string ModuleLoader(string name, IEnumerable<string> searchDirectories);

        private readonly MondState _state;

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
        /// The function used to load modules.
        /// </summary>
        public ModuleLoader Loader { get; set; }

        public RequireLibrary(MondState state)
        {
            _state = state;

            Definitions = "\n";
            SearchDirectories = new[] { "." };
            SearchBesideScript = true;

            Loader = (name, searchDirectories) =>
            {
                var foundModule = searchDirectories
                    .Where(p => p != null)
                    .Select(p => Path.Combine(p, name))
                    .FirstOrDefault(File.Exists);

                if (foundModule == null)
                    throw new MondRuntimeException("require: module could not be found: {0}", name);

                return File.ReadAllText(foundModule);
            };
        } 

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var requireClass = RequireClass.Create(_state, this);
            yield return new KeyValuePair<string, MondValue>("require", requireClass["require"]);
        }
    }

    /// <summary>
    /// Library containing the <c>Char</c> module.
    /// </summary>
    public class CharLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public CharLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var charModule = MondModuleBinder.Bind<CharModule>(_state);
            yield return new KeyValuePair<string, MondValue>("Char", charModule);
        }
    }

    /// <summary>
    /// Library containing the <c>Math</c> module.
    /// </summary>
    public class MathLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public MathLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var mathModule = MondModuleBinder.Bind<MathModule>(_state);

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
        private readonly MondState _state;

        public OperatorLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var operatorModule = MondModuleBinder.Bind<OperatorModule>(_state);
            foreach (var pair in operatorModule.Object)
                yield return new KeyValuePair<string, MondValue>(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Library containing the <c>Random</c> class.
    /// </summary>
    public class RandomLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public RandomLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var randomClass = MondClassBinder.Bind<RandomClass>(_state);
            yield return new KeyValuePair<string, MondValue>("Random", randomClass);
        }
    }
}

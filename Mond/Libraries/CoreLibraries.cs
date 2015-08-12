using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Binding;
using Mond.Libraries.Core;
using Mond.Libraries.Math;

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
        }
    }

    /// <summary>
    /// Library containing the <c>error</c> and <c>try</c> functions.
    /// </summary>
    public class ErrorLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var errorModule = MondModuleBinder.Bind<ErrorModule>();
            yield return new KeyValuePair<string, MondValue>("error", errorModule["error"]);
            yield return new KeyValuePair<string, MondValue>("try", errorModule["try"]);
        }
    }

    /// <summary>
    /// Library containing the <c>require</c> function.
    /// </summary>
    public class RequireLibrary : IMondLibrary
    {
        public delegate string ModuleLoader(string name, IReadOnlyList<string> searchDirectories);

        /// <summary>
        /// The options to use when compiling modules. <c>FirstLineNumber</c> will be set to its proper value.
        /// </summary>
        public MondCompilerOptions Options { get; set; }

        /// <summary>
        /// The definition string from <c>MondLibraryManager</c>. This shouldn't need to be changed.
        /// </summary>
        public string Definitions { get; set; }

        /// <summary>
        /// The function used to load modules.
        /// </summary>
        public ModuleLoader Loader { get; set; }

        public RequireLibrary()
        {
            Definitions = "\n";

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
            var requireClass = RequireClass.Create(this);
            yield return new KeyValuePair<string, MondValue>("require", requireClass["require"]);
        }
    }

    /// <summary>
    /// Library containing the <c>Char</c> module.
    /// </summary>
    public class CharLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var charModule = MondModuleBinder.Bind<CharModule>();
            yield return new KeyValuePair<string, MondValue>("Char", charModule);
        }
    }

    /// <summary>
    /// Library containing the <c>Math</c> module.
    /// </summary>
    public class MathLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var mathModule = MondModuleBinder.Bind<MathModule>();

            mathModule["PI"] = System.Math.PI;
            mathModule["E"] = System.Math.E;

            yield return new KeyValuePair<string, MondValue>("Math", mathModule);
        }
    }

    /// <summary>
    /// Library containing the <c>Random</c> class.
    /// </summary>
    public class RandomLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var randomClass = MondClassBinder.Bind<RandomClass>();
            yield return new KeyValuePair<string, MondValue>("Random", randomClass);
        }
    }
}

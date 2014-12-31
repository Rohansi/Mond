using System;
using System.Collections.Generic;
using System.IO;
using Mond.Binding;
using Mond.Libraries.Core;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains the <c>error</c> and <c>require</c> functions.
    /// </summary>
    public class CoreLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new ErrorLibrary();
            yield return new RequireLibrary();
        }
    }

    /// <summary>
    /// Library containing the <c>error</c> function.
    /// </summary>
    public class ErrorLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var errorModule = MondModuleBinder.Bind<MondError>();
            yield return new KeyValuePair<string, MondValue>("error", errorModule["error"]);
        }
    }

    /// <summary>
    /// Library containing the <c>require</c> function.
    /// </summary>
    public class RequireLibrary : IMondLibrary
    {
        /// <summary>
        /// 
        /// </summary>
        public string Definitions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<string, string> Loader { get; set; }

        public RequireLibrary()
        {
            Definitions = "\n";

            Loader = File.ReadAllText;
        } 

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var requireClass = MondRequire.Create(this);
            yield return new KeyValuePair<string, MondValue>("require", requireClass["require"]);
        }
    }
}

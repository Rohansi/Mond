using System.Collections.Generic;
using Mond.Binding;
using Mond.Libraries.Json;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains all of the JSON related libraries.
    /// </summary>
    public class JsonLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new JsonLibrary(state);
        }
    }

    /// <summary>
    /// Library containing functions for serializing and deserializing JSON.
    /// </summary>
    public class JsonLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public JsonLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var jsonModule = MondModuleBinder.Bind<JsonModule>(_state);
            yield return new KeyValuePair<string, MondValue>("Json", jsonModule);
        }
    }
}

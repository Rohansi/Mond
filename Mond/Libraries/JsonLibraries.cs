using System.Collections.Generic;
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
            yield return new JsonLibrary();
        }
    }

    /// <summary>
    /// Library containing functions for serializing and deserializing JSON.
    /// </summary>
    public class JsonLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)
        {
            return new JsonModule.Library().GetDefinitions(state);
        }
    }
}

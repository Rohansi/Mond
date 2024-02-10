using System.Collections.Generic;
using Mond.Libraries.Json;

namespace Mond.Libraries
{
    /// <summary>
    /// Library containing functions for serializing and deserializing JSON.
    /// </summary>
    public class JsonLibrary : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new JsonModule.Library();
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains all of the libraries defined in Mond.
    /// </summary>
    public class StandardLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            var libraries = new IMondLibraryCollection[]
            {
                new CoreLibraries(),
                new ConsoleLibraries(),
                new JsonLibraries(),
#if !UNITY
                new AsyncLibraries(),
#endif
            };

            return libraries.SelectMany(l => l.Create(state));
        }
    }
}

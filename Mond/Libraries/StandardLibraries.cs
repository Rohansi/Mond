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
                new MathLibraries(), 
                new ConsoleLibraries(),
                new AsyncLibraries(),
                new JsonLibraries()
            };

            return libraries.SelectMany(l => l.Create(state));
        }
    }
}

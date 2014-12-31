using System.Collections.Generic;

namespace Mond.Libraries
{
    public interface IMondLibraryCollection
    {
        IEnumerable<IMondLibrary> Create(MondState state);
    }
}

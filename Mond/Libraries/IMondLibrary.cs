using System.Collections.Generic;

namespace Mond.Libraries
{
    public interface IMondLibrary
    {
        IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state);
    }
}

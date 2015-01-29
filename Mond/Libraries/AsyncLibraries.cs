using System.Collections.Generic;
using Mond.Binding;
using Mond.Libraries.Async;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains all of the async related libraries.
    /// </summary>
    public class AsyncLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new AsyncLibrary();
        }
    }

    public class AsyncLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var asyncClass = AsyncClass.Create();
            yield return new KeyValuePair<string, MondValue>("Async", asyncClass);

            var taskModule = MondModuleBinder.Bind<TaskModule>();
            yield return new KeyValuePair<string, MondValue>("Task", taskModule);
        }
    }
}

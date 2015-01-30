using System.Collections.Generic;
using System.Threading.Tasks;
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

    /// <summary>
    /// Library containing the <c>Async</c> and <c>Task</c> modules.
    /// </summary>
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

    public static class AsyncUtil
    {
        /// <summary>
        /// Runs a Mond sequence as an async function.
        /// Should only be used when implementing your own async methods.
        /// </summary>
        public static async Task<MondValue> RunMondTask(MondState state, MondValue enumerator)
        {
            var input = MondValue.Undefined;

            while (true)
            {
                var yielded = state.Call(enumerator["moveNext"], input);
                var result = enumerator["current"];

                if (!yielded)
                    return result;

                if (result.Type != MondValueType.Object)
                    throw new MondRuntimeException("Tasks may only yield objects");

                var task = result.UserData as Task<MondValue>;
                if (task != null)
                {
                    input = await task;
                    continue;
                }

                var getEnumerator = result["getEnumerator"];
                
                if (getEnumerator.Type != MondValueType.Function)
                    throw new MondRuntimeException("Task objects must define getEnumerator");

                var resultEnumerator = state.Call(getEnumerator);
                input = await RunMondTask(state, resultEnumerator);
            }
        }
    }
}

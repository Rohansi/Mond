using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            yield return new AsyncLibrary(state);
        }
    }

    /// <summary>
    /// Library containing the <c>Async</c>, <c>Task</c>, <c>TaskCompletionSource</c>,
    /// <c>CancellationTokenSource</c>, and <c>CancellationToken</c>.
    /// </summary>
    public class AsyncLibrary : IMondLibrary
    {
        private readonly MondState _state;

        public AsyncLibrary(MondState state) => _state = state;

        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var asyncClass = AsyncClass.Create(_state);
            yield return new KeyValuePair<string, MondValue>("Async", asyncClass);

            var taskModule = MondModuleBinder.Bind<TaskModule>(_state);
            yield return new KeyValuePair<string, MondValue>("Task", taskModule);

            var tcsClass = MondClassBinder.Bind<TaskCompletionSourceClass>(_state);
            yield return new KeyValuePair<string, MondValue>("TaskCompletionSource", tcsClass);

            var ctsClass = MondClassBinder.Bind<CancellationTokenSourceClass>(_state);
            yield return new KeyValuePair<string, MondValue>("CancellationTokenSource", ctsClass);

            var ctClass = MondClassBinder.Bind<CancellationTokenClass>(_state);
            yield return new KeyValuePair<string, MondValue>("CancellationToken", ctClass);
        }
    }

    public static class AsyncUtil
    {
        /// <summary>
        /// Throws an exception if not running in an async function.
        /// </summary>
        public static void EnsureAsync(string message = null)
        {
            if (!(SynchronizationContext.Current is MondSynchronizationContext))
                throw new MondRuntimeException(message ?? "Cannot use async functions in a synchronous context");
        }

        /// <summary>
        /// Runs a Mond sequence as an async function.
        /// Should only be used when implementing your own async methods.
        /// </summary>
        public static async Task<MondValue> RunMondTask(MondState state, MondValue enumerator)
        {
            EnsureAsync();

            var input = MondValue.Undefined;

            while (true)
            {
                EnsureAsync();

                var yielded = state.Call(enumerator["moveNext"], input);
                var result = enumerator["current"];

                if (!yielded)
                    return result;

                if (result.Type != MondValueType.Object)
                    throw new MondRuntimeException("Tasks may only yield objects");

                if (result.UserData is Task<MondValue> task)
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

        /// <summary>
        /// Converts a Task to a MondValue.
        /// </summary>
        public static MondValue ToObject(Task task)
        {
            return ToObject(task.ContinueWith(t => MondValue.Undefined));
        }

        /// <summary>
        /// Converts a Task to a MondValue.
        /// </summary>
        public static MondValue ToObject(Task<MondValue> task)
        {
            var result = MondValue.Object();
            result.Prototype = MondValue.Null;
            result.UserData = task;
            return result;
        }

        [Obsolete("Mond tasks must return MondValue", true)]
        public static void ToObject<T>(Task<T> task)
        {
            // this is just a dummy function that prevents the implicit
            // conversion from Task<T> to Task when T is not MondValue
        }

        /// <summary>
        /// Converts an array of MondValues to an array of Tasks.
        /// </summary>
        public static Task<MondValue>[] ToTaskArray(MondState state, params MondValue[] tasks)
        {
            if (tasks.Length == 1 && tasks[0].Type == MondValueType.Array)
                tasks = tasks[0].AsList.ToArray();

            return tasks
                .Select(t =>
                {
                    if (t.UserData is Task<MondValue> task)
                        return task;

                    return RunMondTask(state, t);
                })
                .ToArray();
        }

        /// <summary>
        /// Tries to convert a MondValue to a CancellationToken. If value is null, this will
        /// return CancellationToken.None.
        /// </summary>
        public static CancellationToken? AsCancellationToken(MondValue value)
        {
            if (value == MondValue.Undefined)
                return CancellationToken.None;

            if (value.Type != MondValueType.Object)
                return null;

            var token = value.UserData as CancellationTokenClass;

            if (token == null)
                return null;

            return token.CancellationToken;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mond.Libraries.Async;

namespace Mond.Libraries
{
    /// <summary>
    /// Library containing <c>Async</c>, <c>Task</c>, <c>TaskCompletionSource</c>,
    /// <c>CancellationTokenSource</c>, and <c>CancellationToken</c>.
    /// </summary>
    public class AsyncLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new AsyncClass.Library();
            yield return new TaskModule.Library();
            yield return new TaskCompletionSourceClass.Library();
            yield return new CancellationTokenSourceClass.Library();
            yield return new CancellationTokenClass.Library();
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
        public static Task<MondValue>[] ToTaskArray(MondState state, params Span<MondValue> tasks)
        {
            if (tasks.Length == 1 && tasks[0].Type == MondValueType.Array)
            {
                tasks = tasks[0].AsList.ToArray();
            }

            var actualTasks = new Task<MondValue>[tasks.Length];
            for (var i = 0; i < tasks.Length; i++)
            {
                actualTasks[i] = tasks[i].UserData as Task<MondValue> ?? RunMondTask(state, tasks[i]);
            }

            return actualTasks;
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

        /// <summary>
        /// Used to rethrow exceptions thrown in async methods that have bindings generated for them.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
        public static MondValue RethrowAsyncException(AggregateException e)
        {
            var exception = e.InnerExceptions.Count != 1 ? e : e.InnerException ?? e;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            throw exception;
        }
    }
}

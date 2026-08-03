using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    /// <summary>
    /// Produces a task whose result is supplied by hand, for bridging callback based code.
    /// </summary>
    [MondClass("TaskCompletionSource")]
    internal partial class TaskCompletionSourceClass
    {
        private readonly TaskCompletionSource<MondValue> _tcs;

        /// <summary>
        /// Creates a source whose task has not completed yet.
        /// </summary>
        [MondConstructor]
        public TaskCompletionSourceClass() => _tcs = new TaskCompletionSource<MondValue>();

        /// <summary>
        /// Returns the task controlled by this source.
        /// </summary>
        [MondFunction]
        public MondValue GetTask() => AsyncUtil.ToObject(_tcs.Task);

        /// <summary>
        /// Completes the task as cancelled.
        /// </summary>
        [MondFunction]
        public void SetCanceled() => _tcs.SetCanceled();

        /// <summary>
        /// Completes the task with an error carrying the given message.
        /// </summary>
        [MondFunction]
        public void SetException(string message) => _tcs.SetException(new MondRuntimeException(message));

        /// <summary>
        /// Completes the task with the given result.
        /// </summary>
        [MondFunction]
        public void SetResult(MondValue result) => _tcs.SetResult(result);
    }
}

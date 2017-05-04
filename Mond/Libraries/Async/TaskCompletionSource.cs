using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("TaskCompletionSource")]
    internal class TaskCompletionSourceClass
    {
        private readonly TaskCompletionSource<MondValue> _tcs;

        [MondConstructor]
        public TaskCompletionSourceClass() => _tcs = new TaskCompletionSource<MondValue>();

        [MondFunction("getTask")]
        public MondValue GetTask() => AsyncUtil.ToObject(_tcs.Task);

        [MondFunction("setCanceled")]
        public void SetCanceled() => _tcs.SetCanceled();

        [MondFunction("setException")]
        public void SetException(string message) => _tcs.SetException(new MondRuntimeException(message));

        [MondFunction("setResult")]
        public void SetResult(MondValue result) => _tcs.SetResult(result);
    }
}

using System;
using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondModule("Task")]
    internal class TaskModule
    {
        [MondFunction]
        public static MondValue Delay(double seconds, MondValue? cancellationToken = null)
        {
            AsyncUtil.EnsureAsync();

            var ct = AsyncUtil.AsCancellationToken(cancellationToken ?? MondValue.Undefined);

            if (!ct.HasValue)
                throw new MondRuntimeException("Task.delay: second argument must be a CancellationToken");

            var timeSpan = seconds >= 0 ? 
                TimeSpan.FromSeconds(seconds) :
                TimeSpan.FromMilliseconds(-1);

            return AsyncUtil.ToObject(Task.Delay(timeSpan, ct.Value));
        }

        [MondFunction]
        public static MondValue WhenAll(MondState state, params MondValue[] tasks)
        {
            AsyncUtil.EnsureAsync();

            var taskArray = AsyncUtil.ToTaskArray(state, tasks);

            var task = Task.WhenAll(taskArray).ContinueWith(t =>
            {
                var array = MondValue.Array();
                array.ArrayValue.AddRange(t.Result);
                return array;
            });

            return AsyncUtil.ToObject(task);
        }

        [MondFunction]
        public static MondValue WhenAny(MondState state, params MondValue[] tasks)
        {
            AsyncUtil.EnsureAsync();

            var taskArray = AsyncUtil.ToTaskArray(state, tasks);

            var task = Task.WhenAny(taskArray).ContinueWith(t =>
            {
                var index = Array.IndexOf(taskArray, t.Result);
                return tasks[index];
            });

            return AsyncUtil.ToObject(task);
        }
    }
}

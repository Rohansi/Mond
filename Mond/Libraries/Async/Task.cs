using System;
using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondModule("Task")]
    internal class TaskModule
    {
        [MondFunction("delay")]
        public static MondValue Delay(double seconds)
        {
            return AsyncUtil.ToObject(Task.Delay(TimeSpan.FromSeconds(seconds)));
        }

        [MondFunction("delay")]
        public static MondValue Delay(double seconds, MondValue cancellationToken)
        {
            var ct = AsyncUtil.AsCancellationToken(cancellationToken);

            if (!ct.HasValue)
                throw new MondRuntimeException("delay: second argument must be a CancellationToken");

            return AsyncUtil.ToObject(Task.Delay(TimeSpan.FromSeconds(seconds), ct.Value));
        }

        [MondFunction("whenAll")]
        public static MondValue WhenAll(MondState state, params MondValue[] tasks)
        {
            var taskArray = AsyncUtil.ToTaskArray(state, tasks);

            var task = Task.WhenAll(taskArray).ContinueWith(t =>
            {
                var array = new MondValue(MondValueType.Array);
                array.ArrayValue.AddRange(t.Result);
                return array;
            });

            return AsyncUtil.ToObject(task);
        }

        [MondFunction("whenAny")]
        public static MondValue WhenAny(MondState state, params MondValue[] tasks)
        {
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

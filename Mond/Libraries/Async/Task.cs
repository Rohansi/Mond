using System;
using System.Linq;
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
            return TaskToObject(Task.Delay(TimeSpan.FromSeconds(seconds)));
        }

        [MondFunction("whenAll")]
        public static MondValue WhenAll(MondState state, params MondValue[] tasks)
        {
            var taskArray = ToTaskArray(state, tasks);

            var task = Task.WhenAll(taskArray).ContinueWith(t =>
            {
                var array = new MondValue(MondValueType.Array);
                array.ArrayValue.AddRange(t.Result);
                return array;
            });

            return TaskToObject(task);
        }

        [MondFunction("whenAny")]
        public static MondValue WhenAny(MondState state, params MondValue[] tasks)
        {
            var taskArray = ToTaskArray(state, tasks);

            var task = Task.WhenAny(taskArray).ContinueWith(t =>
            {
                var index = Array.IndexOf(taskArray, t);
                return tasks[index];
            });

            return TaskToObject(task);
        }

        private static Task<MondValue>[] ToTaskArray(MondState state, params MondValue[] tasks)
        {
            return tasks
                .Select(t =>
                {
                    var task = t.UserData as Task<MondValue>;
                    if (task != null)
                        return task;

                    return AsyncUtil.RunMondTask(state, t);
                })
                .ToArray();
        }

        private static MondValue TaskToObject(Task task)
        {
            return TaskToObject(task.ContinueWith(t => MondValue.Undefined));
        }

        private static MondValue TaskToObject(Task<MondValue> task)
        {
            return new MondValue(MondValueType.Object)
            {
                Prototype = MondValue.Null,
                UserData = task
            };
        }
    }
}

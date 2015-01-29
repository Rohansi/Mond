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
        public static MondValue WhenAll(params MondValue[] tasks)
        {
            return TaskToObject(Task.WhenAll(ToTaskArray(tasks)));
        }

        [MondFunction("whenAny")]
        public static MondValue WhenAny(params MondValue[] tasks)
        {
            return TaskToObject(Task.WhenAny(ToTaskArray(tasks)));
        }

        private static Task<MondValue>[] ToTaskArray(params MondValue[] tasks)
        {
            return tasks
                .Select(t => (Task<MondValue>)t.UserData)
                .ToArray();
        }

        private static MondValue TaskToObject(Task task)
        {
            return new MondValue(MondValueType.Object)
            {
                Prototype = MondValue.Null,
                UserData = task.ContinueWith(t => MondValue.Undefined)
            };
        }
    }
}

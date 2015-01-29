using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("Async")]
    internal class AsyncClass
    {
        private readonly List<NativeTask> _nativeTasks;
        private readonly Queue<MondTask> _tasks;

        private AsyncClass()
        {
            _nativeTasks = new List<NativeTask>();
            _tasks = new Queue<MondTask>();
        }

        public static MondValue Create()
        {
            MondValue prototype;
            MondClassBinder.Bind<AsyncClass>(out prototype);

            var instance = new AsyncClass();

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("start")]
        public void Start(MondState state, MondValue function)
        {
            var enumerator = state.Call(state.Call(function)["getEnumerator"]);

            _tasks.Enqueue(new MondTask(enumerator));
        }

        [MondFunction("runToCompletion")]
        public void RunToCompletion(MondState state)
        {
            while (_nativeTasks.Count > 0 || _tasks.Count > 0)
            {
                if (_nativeTasks.Count > 0)
                {
                    if (_tasks.Count > 0)
                    {
                        RunNativeTask(true);
                        RunSomeTasks(state);
                    }
                    else
                    {
                        RunNativeTask(false);
                    }
                }
                else
                {
                    RunSomeTasks(state);
                }
            }
        }

        private void RunNativeTask(bool timeout)
        {
            var whenAnyTask = Task.WhenAny(_nativeTasks.Select(t => t.Task));

            if (timeout && !whenAnyTask.Wait(0))
                return;

            var task = whenAnyTask.Result;

            var nativeTask = _nativeTasks.Find(t => ReferenceEquals(t.Task, task));
            _nativeTasks.Remove(nativeTask);

            var mondTask = nativeTask.Parent;

            mondTask.Result = task.Result;
            _tasks.Enqueue(mondTask);
        }

        private void RunSomeTasks(MondState state)
        {
            var count = _tasks.Count;

            while (count >= 0 && _tasks.Count > 0)
            {
                count--;

                var mondTask = _tasks.Dequeue();
                var enumerator = mondTask.Enumerator;

                var yielded = state.Call(enumerator["moveNext"], mondTask.Result);
                var result = enumerator["current"];

                if (yielded)
                {
                    if (result.Type == MondValueType.Object)
                    {
                        var task = result.UserData as Task<MondValue>;
                        if (task != null)
                        {
                            _nativeTasks.Add(new NativeTask(task, mondTask));
                            continue;
                        }
                    }

                    var parent = mondTask;
                    var resultEnumerator = state.Call(result["getEnumerator"]);

                    mondTask = new MondTask(resultEnumerator, parent);
                }
                else
                {
                    var parent = mondTask.Parent;

                    if (parent == null)
                        continue;

                    mondTask = parent;
                    mondTask.Result = result;
                }

                _tasks.Enqueue(mondTask);
            }
        }

        class NativeTask
        {
            public readonly Task<MondValue> Task;
            public readonly MondTask Parent;

            public NativeTask(Task<MondValue> task, MondTask parent)
            {
                Task = task;
                Parent = parent;
            }
        }

        class MondTask
        {
            public readonly MondValue Enumerator;
            public readonly MondTask Parent;

            public MondValue Result;

            public MondTask(MondValue enumerator, MondTask parent = null)
            {
                Enumerator = enumerator;
                Parent = parent;

                Result = MondValue.Undefined;
            }
        }
    }
}

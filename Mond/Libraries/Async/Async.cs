using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondModule("Async")]
    internal partial class AsyncClass
    {
        private readonly MondTaskScheduler _scheduler;
        private readonly TaskFactory _factory;
        private int _activeTasks;

        private readonly Queue<Exception> _exceptions;

        private AsyncClass()
        {
            _scheduler = new MondTaskScheduler();
            _factory = new TaskFactory(_scheduler);
            _activeTasks = 0;

            _exceptions = new Queue<Exception>();
        }

        [MondFunction]
        public MondValue Start(MondState state, MondValue value)
        {
            if (value.Type == MondValueType.Function)
                value = state.Call(value);

            var getEnumerator = value["getEnumerator"];

            if (getEnumerator.Type != MondValueType.Function)
                throw new MondRuntimeException("Task objects must define getEnumerator");

            var enumerator = state.Call(getEnumerator);

            var task = _factory.StartNew(async () =>
            {
                try
                {
                    await AsyncUtil.RunMondTask(state, enumerator);
                }
                catch (Exception e)
                {
                    lock (_exceptions)
                        _exceptions.Enqueue(e);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeTasks);
                }
            });

            Interlocked.Increment(ref _activeTasks);

            // return a task that completes when the started task completes
            Func<Task> waitTask = async () =>
            {
                await await task;
            };

            return AsyncUtil.ToObject(waitTask());
        }

        [MondFunction]
        public bool Run()
        {
            if (SynchronizationContext.Current is MondSynchronizationContext)
                throw new MondRuntimeException("Async.run: cannot be called in an async function");

            Exception ex = null;

            lock (_exceptions)
            {
                if (_exceptions.Count > 0)
                    ex = _exceptions.Dequeue();
            }

            if (ex != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Unhandled error in task:");
                sb.Append(ex.Message);

                throw new MondRuntimeException(sb.ToString(), ex);
            }

            _scheduler.Run();

            lock (_exceptions)
                return _activeTasks > 0 || _exceptions.Count > 0;
        }

        [MondFunction]
        public void RunToCompletion()
        {
            var waitTask = Task.Run(async () =>
            {
                while (Run())
                {
                    await Task.Delay(1);
                }
            });

            waitTask.Wait();
        }
    }
}

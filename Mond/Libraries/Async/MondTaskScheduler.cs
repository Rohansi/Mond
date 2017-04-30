#if !UNITY

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mond.Libraries.Async
{
    internal class MondTaskScheduler : TaskScheduler
    {
        private readonly MondSynchronizationContext _syncContext;
        private ConcurrentQueue<Task> _tasks;
        private ConcurrentQueue<Tuple<SendOrPostCallback, object>> _callbacks;

        public MondTaskScheduler()
        {
            _syncContext = new MondSynchronizationContext(this);
            _tasks = new ConcurrentQueue<Task>();
            _callbacks = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
        }

        public void Run()
        {
            var originalSyncContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(_syncContext);

            try
            {
                var count = _tasks.Count + _callbacks.Count;

                if (count == 0)
                    return;

                while (count-- > 0)
                {
                    if (_callbacks.TryDequeue(out var callback))
                        callback.Item1(callback.Item2);

                    if (!_tasks.TryDequeue(out var task))
                        break;

                    TryExecuteTask(task);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalSyncContext);
            }
        }

        internal void PostCallback(SendOrPostCallback d, object state)
        {
            _callbacks.Enqueue(Tuple.Create(d, state));
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Enqueue(task);
        }

        protected override bool TryDequeue(Task task)
        {
            return false;
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued && !TryDequeue(task))
                return false;

            return TryExecuteTask(task);
        }
    }

    internal class MondSynchronizationContext : SynchronizationContext
    {
        private readonly MondTaskScheduler _scheduler;

        public MondSynchronizationContext(MondTaskScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new MondSynchronizationContext(_scheduler);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _scheduler.PostCallback(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("MondSynchronizationContext.Send");
        }
    }
}

#endif

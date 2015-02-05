using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mond.Libraries.Async
{
    internal class Scheduler : TaskScheduler
    {
        private ConcurrentQueue<Task> _tasks;

        public Scheduler()
        {
            _tasks = new ConcurrentQueue<Task>();
        }

        public void Run()
        {
            var count = _tasks.Count;

            if (count == 0)
                return;

            while (--count >= 0)
            {
                Task task;
                if (!_tasks.TryDequeue(out task))
                    break;

                TryExecuteTask(task);
            }
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
}

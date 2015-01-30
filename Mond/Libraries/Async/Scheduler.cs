using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mond.Libraries.Async
{
    internal class Scheduler : TaskScheduler
    {
        private List<Task> _tasks;

        public Scheduler()
        {
            _tasks = new List<Task>();
        }

        public void Run()
        {
            int count;

            lock (_tasks)
            {
                count = _tasks.Count;
            }

            if (count == 0)
                return;

            while (--count >= 0)
            {
                Task task;

                lock (_tasks)
                {
                    if (_tasks.Count == 0)
                        return;

                    task = _tasks[0];
                    _tasks.RemoveAt(0);
                }

                if (!TryExecuteTask(task))
                    _tasks.Add(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (_tasks)
            {
                return _tasks.ToArray();
            }
        }

        protected override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.Add(task);
            }
        }

        protected override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }
}

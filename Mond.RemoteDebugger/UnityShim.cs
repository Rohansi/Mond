#if UNITY

using System.Threading;

namespace Mond.RemoteDebugger
{
    class SemaphoreSlim
    {
        private readonly Semaphore _semaphore;
        private int _currentCount;

        public SemaphoreSlim(int initialCount)
        {
            _semaphore = new Semaphore(initialCount, initialCount);
            _currentCount = initialCount;
        }

        public int CurrentCount
        {
            get { return _currentCount; }
        }

        public void Wait()
        {
            _semaphore.WaitOne();
            Interlocked.Decrement(ref _currentCount);
        }

        public void Release()
        {
            _semaphore.Release();
            Interlocked.Increment(ref _currentCount);
        }
    }

    class Task<T>
    {
        private TaskCompletionSource<T> _tcs;

        public Task(TaskCompletionSource<T> tcs)
        {
            _tcs = tcs;
        }

        public T Result
        {
            get { return _tcs.Result; }
        }
    }

    class TaskCompletionSource<T>
    {
        private readonly AutoResetEvent _event;
        private T _value;

        public TaskCompletionSource()
        {
            _event = new AutoResetEvent(false);
        }

        public Task<T> Task
        {
            get { return new Task<T>(this); }
        } 

        public void SetResult(T value)
        {
            _value = value;
            _event.Set();
        }

        public T Result
        {
            get
            {
                _event.WaitOne();
                return _value;
            }
        }
    }
}

#endif
